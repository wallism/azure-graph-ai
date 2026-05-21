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

    [NodeRelationship<UserAssignedManagedIdentity>("USES_MANAGED_IDENTITY")]
    public List<string> UserAssignedManagedIdentities { get; set; } = [];

    [JsonProperty("identity", NullValueHandling = NullValueHandling.Ignore)]
    public AzureIdentity? Identity { get; set; }

    [NodeProperty("identityType")]
    [JsonIgnore]
    public string? IdentityType => Identity?.Type;

    [NodeProperty("identityPrincipalId")]
    [JsonIgnore]
    public Guid? IdentityPrincipalId => Identity?.PrincipalId;

    [NodeProperty("identityTenantId")]
    [JsonIgnore]
    public Guid? IdentityTenantId => Identity?.TenantId;

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
