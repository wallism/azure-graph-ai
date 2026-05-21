using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public sealed class Sku
{
    [NodeProperty("skuName")]
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [NodeProperty("skuTier")]
    [JsonProperty("tier", NullValueHandling = NullValueHandling.Ignore)]
    public string? Tier { get; set; }

    [NodeProperty("skuCapacity")]
    [JsonProperty("capacity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Capacity { get; set; }

    [JsonProperty("family", NullValueHandling = NullValueHandling.Ignore)]
    public string? Family { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public string? Size { get; set; }
}

public sealed class AzureIdentity
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("principalId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? PrincipalId { get; set; }

    [JsonProperty("tenantId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? TenantId { get; set; }

    [JsonProperty("userAssignedIdentities", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, UserAssignedIdentityReference> UserAssignedIdentities { get; set; } = [];
}

public sealed class UserAssignedIdentityReference
{
    [JsonProperty("principalId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? PrincipalId { get; set; }

    [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? ClientId { get; set; }
}

public sealed class SystemData
{
    [JsonProperty("createdByType", NullValueHandling = NullValueHandling.Ignore)]
    public string? CreatedByType { get; set; }

    [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("lastModifiedByType", NullValueHandling = NullValueHandling.Ignore)]
    public string? LastModifiedByType { get; set; }

    [JsonProperty("lastModifiedAt", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? LastModifiedAt { get; set; }
}
