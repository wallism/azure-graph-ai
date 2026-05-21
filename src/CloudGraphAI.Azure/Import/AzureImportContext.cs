using CloudGraphAI.Azure.Environments;
using CloudGraphAI.Azure.Models;

namespace CloudGraphAI.Azure.Import;

public sealed class AzureImportContext(IReadOnlyList<string> subscriptionIds, IResourceEnvironmentResolver environmentResolver)
{
    private readonly Dictionary<Type, List<AzureGraphNode>> _nodesByType = [];

    public IReadOnlyList<string> SubscriptionIds { get; } = subscriptionIds;

    public void AddNodes<T>(IEnumerable<T> nodes)
        where T : AzureGraphNode
    {
        var list = nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).Cast<AzureGraphNode>().ToList();
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
        where T : AzureGraphNode
        => _nodesByType.TryGetValue(typeof(T), out var nodes) ? nodes.OfType<T>().ToList() : [];

    public IEnumerable<(Type Type, IReadOnlyList<AzureGraphNode> Nodes)> GetNodeGroups()
        => _nodesByType.Select(pair => (pair.Key, (IReadOnlyList<AzureGraphNode>)pair.Value));

    public IEnumerable<AzureGraphNode> GetAllNodes()
        => _nodesByType.Values.SelectMany(nodes => nodes);

    public T? FindById<T>(string? id)
        where T : AzureGraphNode
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return GetNodes<T>().FirstOrDefault(node => node.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public T? FindByName<T>(string? name)
        where T : AzureGraphNode
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return GetNodes<T>().FirstOrDefault(node => node.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
    }

    public T? Find<T>(Func<T, bool> predicate)
        where T : AzureGraphNode
        => GetNodes<T>().FirstOrDefault(predicate);

    public void ApplyCommonResourceLinks(AzureResourceNode resource)
    {
        var resourceGroupId = AzureResourceId.GetResourceGroupId(resource.Id);
        if (!string.IsNullOrWhiteSpace(resourceGroupId) && resource is not ResourceGroup)
            resource.ResourceGroups = [resourceGroupId];

        var subscriptionId = AzureResourceId.GetSubscriptionId(resource.Id);
        if (!string.IsNullOrWhiteSpace(subscriptionId))
            resource.Subscriptions = [AzureResourceId.BuildSubscriptionId(subscriptionId)];

        resource.Environment = environmentResolver.Resolve(resource);
    }
}
