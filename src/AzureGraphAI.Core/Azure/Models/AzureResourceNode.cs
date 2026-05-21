using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public interface IEnvironmentAnnotatedResource
{
    string? Environment { get; set; }
}

public abstract class AzureResourceNode : AzureGraphNode, IEnvironmentAnnotatedResource
{
    [NodeRelationship<ResourceGroup>("IN_GROUP")]
    public List<string> ResourceGroups { get; set; } = [];

    [NodeRelationship<Subscription>("IN_SUBSCRIPTION")]
    public List<string> Subscriptions { get; set; } = [];

    [NodeProperty("subscriptionId")]
    [JsonIgnore]
    public string? SubscriptionId => AzureResourceId.GetSubscriptionId(Id);

    [NodeProperty("resourceGroupName")]
    [JsonIgnore]
    public string? ResourceGroupName => AzureResourceId.GetResourceGroupName(Id);

    [NodeProperty("environment")]
    [JsonIgnore]
    public string? Environment { get; set; }
}
