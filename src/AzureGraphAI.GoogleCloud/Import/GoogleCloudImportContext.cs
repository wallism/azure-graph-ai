using AzureGraphAI.GoogleCloud.Configuration;
using AzureGraphAI.GoogleCloud.Models;

namespace AzureGraphAI.GoogleCloud.Import;

public sealed class GoogleCloudImportContext(IReadOnlyList<string> scopes, GoogleCloudGraphOptions options)
{
    private readonly Dictionary<Type, List<GoogleCloudGraphNode>> _nodesByType = [];

    public IReadOnlyList<string> Scopes { get; } = scopes;

    public void AddNodes<T>(IEnumerable<T> nodes)
        where T : GoogleCloudGraphNode
    {
        var list = nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).Cast<GoogleCloudGraphNode>().ToList();
        if (list.Count == 0)
            return;

        var type = typeof(T);
        if (!_nodesByType.TryGetValue(type, out var existing))
        {
            _nodesByType[type] = list;
            return;
        }

        foreach (var node in list)
        {
            if (existing.All(e => !e.Id.Equals(node.Id, StringComparison.OrdinalIgnoreCase)))
                existing.Add(node);
        }
    }

    public IReadOnlyList<T> GetNodes<T>()
        where T : GoogleCloudGraphNode
        => _nodesByType.TryGetValue(typeof(T), out var nodes) ? nodes.OfType<T>().ToList() : [];

    public IEnumerable<(Type Type, IReadOnlyList<GoogleCloudGraphNode> Nodes)> GetNodeGroups()
        => _nodesByType.Select(pair => (pair.Key, (IReadOnlyList<GoogleCloudGraphNode>)pair.Value));

    public IEnumerable<GoogleCloudGraphNode> GetAllNodes()
        => _nodesByType.Values.SelectMany(nodes => nodes);

    public T? FindById<T>(string? id)
        where T : GoogleCloudGraphNode
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return GetNodes<T>().FirstOrDefault(node => node.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public T? Find<T>(Func<T, bool> predicate)
        where T : GoogleCloudGraphNode
        => GetNodes<T>().FirstOrDefault(predicate);

    public void ApplyCommonResourceLinks(GoogleCloudResourceNode resource, GoogleCloudAsset asset)
    {
        resource.ProjectId ??= GoogleCloudResourceNames.GetProjectId(resource.Id)
            ?? GoogleCloudResourceNames.GetProjectId(resource.ResourceUrl);

        foreach (var ancestor in asset.Ancestors)
        {
            if (ancestor.StartsWith("projects/", StringComparison.OrdinalIgnoreCase))
            {
                resource.ProjectNumber ??= ancestor["projects/".Length..];
                AddDistinct(resource.Projects, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
            }
            else if (ancestor.StartsWith("folders/", StringComparison.OrdinalIgnoreCase))
            {
                AddDistinct(resource.Folders, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
            }
            else if (ancestor.StartsWith("organizations/", StringComparison.OrdinalIgnoreCase))
            {
                AddDistinct(resource.Organizations, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
            }
        }

        if (!string.IsNullOrWhiteSpace(resource.ProjectId))
            AddDistinct(resource.Projects, GoogleCloudResourceNames.BuildCloudResourceManagerName($"projects/{resource.ProjectId}"));

        resource.Environment = ResolveEnvironment(resource);
    }

    public void ApplyHierarchyLinks(GoogleCloudGraphNode node, GoogleCloudAsset asset)
    {
        if (node is GoogleProject project)
        {
            foreach (var ancestor in asset.Ancestors)
            {
                if (ancestor.StartsWith("folders/", StringComparison.OrdinalIgnoreCase))
                    AddDistinct(project.Folders, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
                else if (ancestor.StartsWith("organizations/", StringComparison.OrdinalIgnoreCase))
                    AddDistinct(project.Organizations, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
            }
        }

        if (node is GoogleFolder folder)
        {
            foreach (var ancestor in asset.Ancestors.Where(a => !GoogleCloudResourceNames.BuildCloudResourceManagerName(a).Equals(folder.Id, StringComparison.OrdinalIgnoreCase)))
            {
                if (ancestor.StartsWith("folders/", StringComparison.OrdinalIgnoreCase))
                    AddDistinct(folder.ParentFolders, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
                else if (ancestor.StartsWith("organizations/", StringComparison.OrdinalIgnoreCase))
                    AddDistinct(folder.Organizations, GoogleCloudResourceNames.BuildCloudResourceManagerName(ancestor));
            }
        }
    }

    private string? ResolveEnvironment(GoogleCloudResourceNode resource)
    {
        foreach (var rule in options.EnvironmentRules)
        {
            if (!string.IsNullOrWhiteSpace(rule)
                && (resource.Id.Contains(rule, StringComparison.OrdinalIgnoreCase)
                    || resource.Name?.Contains(rule, StringComparison.OrdinalIgnoreCase) == true
                    || resource.Labels?.Values.Any(value => value?.Equals(rule, StringComparison.OrdinalIgnoreCase) == true) == true))
            {
                return rule;
            }
        }

        return null;
    }

    private static void AddDistinct(ICollection<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            values.Add(value);
    }
}
