using System.Collections;
using System.Reflection;
using CloudGraphAI.Azure.Environments;
using CloudGraphAI.Azure.Models;
using CloudGraphAI.Azure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4jLiteRepo;
using Neo4jLiteRepo.Attributes;

namespace CloudGraphAI.Azure.Import;

public interface IAzureGraphImportService
{
    Task<AzureGraphImportSummary> ImportAsync(CancellationToken cancellationToken = default);
}

public sealed record AzureGraphImportSummary(
    IReadOnlyDictionary<string, int> NodeCounts,
    IReadOnlyList<DanglingRelationship> DanglingRelationships);

public sealed record DanglingRelationship(
    string SourceLabel,
    string SourceId,
    string Relationship,
    string TargetLabel,
    string TargetId);

public sealed class AzureGraphImportService(
    IConfiguration configuration,
    IResourceEnvironmentResolver environmentResolver,
    IEnumerable<IAzureResourceCollector> collectors,
    INeo4jGenericRepo graphRepo,
    ILogger<AzureGraphImportService> logger)
    : IAzureGraphImportService
{
    private static readonly MethodInfo UpsertNodesMethod = typeof(INeo4jGenericRepo)
        .GetMethods()
        .Single(method => method.Name == nameof(INeo4jGenericRepo.UpsertNodes)
            && method.IsGenericMethodDefinition
            && method.GetParameters().Length == 1);

    private static readonly MethodInfo UpsertRelationshipsMethod = typeof(INeo4jGenericRepo)
        .GetMethods()
        .Single(method => method.Name == nameof(INeo4jGenericRepo.UpsertRelationshipsAsync)
            && method.IsGenericMethodDefinition
            && method.GetParameters().Length == 1
            && method.GetParameters()[0].ParameterType.IsGenericType);

    public async Task<AzureGraphImportSummary> ImportAsync(CancellationToken cancellationToken = default)
    {
        var subscriptionIds = LoadSubscriptionIds();
        if (subscriptionIds.Count == 0)
            throw new InvalidOperationException("No subscriptions configured. Set AzureGraph:IncludedSubscriptions.");

        var context = new AzureImportContext(subscriptionIds, environmentResolver);
        var orderedCollectors = collectors.OrderBy(c => c.Order).ToList();

        foreach (var collector in orderedCollectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Collecting Azure resources with {Collector}", collector.Name);
            var collected = await collector.CollectAsync(context, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Collected {Count} nodes with {Collector}", collected.Count, collector.Name);
        }

        foreach (var collector in orderedCollectors)
            await collector.BuildRelationshipsAsync(context, cancellationToken).ConfigureAwait(false);

        var danglingRelationships = ValidateAndPruneDanglingRelationships(context);

        await graphRepo.EnforceUniqueConstraintsForAllGraphNodes([typeof(AzureGraphNode).Assembly]).ConfigureAwait(false);

        foreach (var group in context.GetNodeGroups())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InvokeRepoListMethodAsync(UpsertNodesMethod, group.Type, group.Nodes).ConfigureAwait(false);
        }

        foreach (var group in context.GetNodeGroups())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InvokeRepoListMethodAsync(UpsertRelationshipsMethod, group.Type, group.Nodes).ConfigureAwait(false);
        }

        var counts = context.GetNodeGroups()
            .OrderBy(group => group.Type.Name)
            .ToDictionary(group => group.Type.Name, group => group.Nodes.Count);

        return new AzureGraphImportSummary(counts, danglingRelationships);
    }

    private List<string> LoadSubscriptionIds()
        => AzureGraphSubscriptionConfiguration.LoadSubscriptionIds(configuration);

    private Task InvokeRepoListMethodAsync(MethodInfo genericMethod, Type nodeType, IReadOnlyList<AzureGraphNode> nodes)
    {
        if (nodes.Count == 0)
            return Task.CompletedTask;

        var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(nodeType))!;
        foreach (var node in nodes)
            typedList.Add(node);

        var task = genericMethod.MakeGenericMethod(nodeType).Invoke(graphRepo, [typedList]) as Task;
        return task ?? throw new InvalidOperationException($"Failed to invoke {genericMethod.Name} for {nodeType.Name}.");
    }

    private IReadOnlyList<DanglingRelationship> ValidateAndPruneDanglingRelationships(AzureImportContext context)
    {
        var targetIndexes = context.GetNodeGroups()
            .ToDictionary(
                group => group.Type,
                group => group.Nodes
                    .Select(node => node.GetPrimaryKeyValue())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

        var dangling = new List<DanglingRelationship>();

        foreach (var node in context.GetAllNodes())
        {
            foreach (var relationship in GetRelationshipProperties(node.GetType()))
            {
                if (!targetIndexes.TryGetValue(relationship.TargetType, out var targetIds))
                    targetIds = [];

                var relationshipIds = GetRelationshipIds(node, relationship.Property).ToList();
                if (relationshipIds.Count == 0)
                    continue;

                var validIds = relationshipIds
                    .Where(targetIds.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var missingIds = relationshipIds
                    .Where(id => !targetIds.Contains(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var missingId in missingIds)
                {
                    dangling.Add(new DanglingRelationship(
                        node.LabelName,
                        node.GetPrimaryKeyValue(),
                        relationship.RelationshipName,
                        relationship.TargetType.Name,
                        missingId));
                }

                if (missingIds.Count > 0)
                {
                    logger.LogWarning(
                        "Pruned {Count} dangling {SourceLabel}-[{Relationship}]->{TargetLabel} relationship ids from {SourceId}",
                        missingIds.Count,
                        node.LabelName,
                        relationship.RelationshipName,
                        relationship.TargetType.Name,
                        node.GetPrimaryKeyValue());
                    SetRelationshipIds(node, relationship.Property, validIds);
                }
            }
        }

        if (dangling.Count > 0)
        {
            logger.LogWarning(
                "Pruned {Count} dangling relationship ids before writing edges. Nodes will still be imported.",
                dangling.Count);
        }

        return dangling;
    }

    private static IEnumerable<RelationshipProperty> GetRelationshipProperties(Type nodeType)
    {
        foreach (var property in nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(string) || !typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                continue;

            var attribute = property.GetCustomAttributes()
                .FirstOrDefault(attr => attr.GetType().IsGenericType
                    && attr.GetType().GetGenericTypeDefinition() == typeof(NodeRelationshipAttribute<>));

            if (attribute is null)
                continue;

            var relationshipName = attribute.GetType().GetProperty(nameof(NodeRelationshipAttribute<GraphNode>.RelationshipName))?.GetValue(attribute)?.ToString();
            if (string.IsNullOrWhiteSpace(relationshipName))
                continue;

            yield return new RelationshipProperty(
                property,
                attribute.GetType().GetGenericArguments()[0],
                relationshipName);
        }
    }

    private static IEnumerable<string> GetRelationshipIds(GraphNode node, PropertyInfo property)
        => property.GetValue(node) is IEnumerable<string> ids
            ? ids.Where(id => !string.IsNullOrWhiteSpace(id))
            : [];

    private static void SetRelationshipIds(GraphNode node, PropertyInfo property, List<string> ids)
    {
        if (property.CanWrite)
        {
            property.SetValue(node, ids);
            return;
        }

        if (property.GetValue(node) is ICollection<string> collection)
        {
            collection.Clear();
            foreach (var id in ids)
                collection.Add(id);
        }
    }

    private sealed record RelationshipProperty(PropertyInfo Property, Type TargetType, string RelationshipName);
}
