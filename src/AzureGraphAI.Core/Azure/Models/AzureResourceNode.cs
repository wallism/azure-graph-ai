using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public interface IEnvironmentAnnotatedResource
{
    string? Environment { get; set; }
}

public abstract class AzureResourceNode : AzureGraphNode, IEnvironmentAnnotatedResource
{
    [NodeRelationship<ResourceGroup>(CommonEdges.InGroup)]
    public List<string> ResourceGroups { get; set; } = [];

    [NodeRelationship<Subscription>(CommonEdges.InSubscription)]
    public List<string> Subscriptions { get; set; } = [];

    [NodeRelationship<UserAssignedManagedIdentity>(CommonEdges.UsesManagedIdentity)]
    public List<string> UserAssignedManagedIdentities { get; set; } = [];

    [JsonProperty("identity", NullValueHandling = NullValueHandling.Ignore)]
    public AzureIdentity? Identity { get; set; }

    [NodeProperty("identityType")]
    [JsonIgnore]
    public string? IdentityType => Identity?.Type;

    [NodeProperty("identityPrincipalId")]
    [JsonIgnore]
    public string? IdentityPrincipalId => Identity?.PrincipalId?.ToString();

    [NodeProperty("identityTenantId")]
    [JsonIgnore]
    public string? IdentityTenantId => Identity?.TenantId?.ToString();

    [NodeProperty("subscriptionId")]
    [JsonIgnore]
    public string? SubscriptionId => AzureResourceId.GetSubscriptionId(Id);

    [NodeProperty("resourceGroupName")]
    [JsonIgnore]
    public string? ResourceGroupName => AzureResourceId.GetResourceGroupName(Id);

    [NodeProperty("environment")]
    [JsonIgnore]
    public string? Environment { get; set; }

    public static class CommonEdges
    {
        public const string InGroup = "IN_GROUP";
        public const string InSubscription = "IN_SUBSCRIPTION";
        public const string UsesManagedIdentity = "USES_MANAGED_IDENTITY";
    }
}
