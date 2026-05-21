using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public sealed class Subscription : AzureGraphNode
{
    [NodeProperty("subscriptionId")]
    [JsonProperty("subscriptionId", NullValueHandling = NullValueHandling.Ignore)]
    public string? AzureSubscriptionId { get; set; }

    [JsonProperty("tenantId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? TenantId { get; set; }

    [NodeProperty("tenantId")]
    [JsonIgnore]
    public string? TenantIdValue => TenantId?.ToString();

    [NodeProperty("subscriptionState")]
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }

    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? AzureDisplayName { get; set; }

    public override string BuildDisplayName()
        => AzureDisplayName ?? AzureSubscriptionId ?? base.BuildDisplayName();
}
