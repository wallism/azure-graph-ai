using Agile.API.Clients.CallHandling;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Logging;

namespace CloudGraphAI.Azure.Import;

public sealed class SubscriptionCollector(
    IAzureRestApi azureApi,
    ILogger<SubscriptionCollector> logger)
    : IAzureResourceCollector
{
    public int Order => 10;

    public string Name => nameof(Subscription);

    public async Task<IReadOnlyList<AzureGraphNode>> CollectAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        var subscriptions = new List<Subscription>();
        foreach (var subscriptionId in context.SubscriptionIds)
        {
            var result = await azureApi.GetSubscriptionAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
            if (!result.WasSuccessful || result.Value is null)
                throw new InvalidOperationException($"Failed to load subscription {subscriptionId}: {result.Exception?.Message ?? result.RawText}");

            var subscription = result.Value;
            subscription.AzureSubscriptionId ??= subscriptionId;
            subscription.Id = AzureResourceId.BuildSubscriptionId(subscriptionId);
            subscription.Type ??= "Microsoft.Resources/subscriptions";
            subscription.Name ??= subscription.AzureDisplayName ?? subscriptionId;
            subscriptions.Add(subscription);
            logger.LogInformation("Loaded subscription {Subscription}", subscription.DisplayName);
        }

        context.AddNodes(subscriptions);
        return subscriptions;
    }

    public Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class ResourceGroupCollector(
    IAzureRestApi azureApi,
    ILogger<ResourceGroupCollector> logger)
    : AzureResourceCollectorBase<ResourceGroup>(azureApi, logger)
{
    public override int Order => 20;

    protected override Task<CallResult<AzureResourceListResult<ResourceGroup>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetResourceGroupsAsync(subscriptionId, cancellationToken);
}

public sealed class StorageAccountCollector(
    IAzureRestApi azureApi,
    ILogger<StorageAccountCollector> logger)
    : AzureResourceCollectorBase<StorageAccount>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<StorageAccount>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetStorageAccountsAsync(subscriptionId, cancellationToken);
}

public sealed class KeyVaultCollector(
    IAzureRestApi azureApi,
    ILogger<KeyVaultCollector> logger)
    : AzureResourceCollectorBase<KeyVault>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<KeyVault>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetKeyVaultsAsync(subscriptionId, cancellationToken);
}

public sealed class UserAssignedManagedIdentityCollector(
    IAzureRestApi azureApi,
    ILogger<UserAssignedManagedIdentityCollector> logger)
    : AzureResourceCollectorBase<UserAssignedManagedIdentity>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<UserAssignedManagedIdentity>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetUserAssignedManagedIdentitiesAsync(subscriptionId, cancellationToken);

    public override Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var resource in context.GetAllNodes().OfType<AzureResourceNode>())
        {
            var identityIds = resource.Identity?.UserAssignedIdentities.Keys
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            foreach (var identityId in identityIds)
            {
                var identity = context.FindById<UserAssignedManagedIdentity>(identityId);
                var relationshipId = identity?.Id ?? identityId;
                if (!resource.UserAssignedManagedIdentities.Contains(relationshipId, StringComparer.OrdinalIgnoreCase))
                    resource.UserAssignedManagedIdentities.Add(relationshipId);
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class ServerFarmCollector(
    IAzureRestApi azureApi,
    ILogger<ServerFarmCollector> logger)
    : AzureResourceCollectorBase<ServerFarm>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<ServerFarm>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetServerFarmsAsync(subscriptionId, cancellationToken);
}

public sealed class ContainerRegistryCollector(
    IAzureRestApi azureApi,
    ILogger<ContainerRegistryCollector> logger)
    : AzureResourceCollectorBase<ContainerRegistry>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<ContainerRegistry>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetContainerRegistriesAsync(subscriptionId, cancellationToken);
}

public sealed class CosmosDbAccountCollector(
    IAzureRestApi azureApi,
    ILogger<CosmosDbAccountCollector> logger)
    : AzureResourceCollectorBase<CosmosDbAccount>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<CosmosDbAccount>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetCosmosDbAccountsAsync(subscriptionId, cancellationToken);
}

public sealed class AzureAIFoundryAccountCollector(
    IAzureRestApi azureApi,
    ILogger<AzureAIFoundryAccountCollector> logger)
    : AzureResourceCollectorBase<AzureAIFoundryAccount>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<AzureAIFoundryAccount>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetAzureAIFoundryAccountsAsync(subscriptionId, cancellationToken);
}

public sealed class RedisCacheCollector(
    IAzureRestApi azureApi,
    ILogger<RedisCacheCollector> logger)
    : AzureResourceCollectorBase<RedisCache>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<RedisCache>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetRedisCachesAsync(subscriptionId, cancellationToken);
}

public sealed class SqlManagedInstanceCollector(
    IAzureRestApi azureApi,
    ILogger<SqlManagedInstanceCollector> logger)
    : AzureResourceCollectorBase<SqlManagedInstance>(azureApi, logger)
{
    public override int Order => 50;

    protected override Task<CallResult<AzureResourceListResult<SqlManagedInstance>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetSqlManagedInstancesAsync(subscriptionId, cancellationToken);

    public override Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var sqlMi in context.GetNodes<SqlManagedInstance>())
        {
            if (!string.IsNullOrWhiteSpace(sqlMi.Properties?.SubnetId))
                sqlMi.DeployedInSubnets = [sqlMi.Properties.SubnetId];
        }

        return Task.CompletedTask;
    }
}
