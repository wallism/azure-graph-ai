using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class ServerFarm : AzureResourceNode
{
    [NodeProperty("kind")]
    [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kind { get; set; }

    [NodeProperty("")]
    [JsonProperty("sku", NullValueHandling = NullValueHandling.Ignore)]
    public Sku? Sku { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public ServerFarmProperties? Properties { get; set; }
}

public sealed class UserAssignedManagedIdentity : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public UserAssignedManagedIdentityProperties? Properties { get; set; }

    [NodeProperty("principalId")]
    [JsonIgnore]
    public string? PrincipalId => Properties?.PrincipalId?.ToString();

    [NodeProperty("clientId")]
    [JsonIgnore]
    public string? ClientId => Properties?.ClientId?.ToString();

    [NodeProperty("tenantId")]
    [JsonIgnore]
    public string? TenantId => Properties?.TenantId?.ToString();

    [NodeProperty("isolationScope")]
    [JsonIgnore]
    public string? IsolationScope => Properties?.IsolationScope;
}

public sealed class UserAssignedManagedIdentityProperties
{
    [JsonProperty("principalId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? PrincipalId { get; set; }

    [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? ClientId { get; set; }

    [JsonProperty("tenantId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? TenantId { get; set; }

    [JsonProperty("isolationScope", NullValueHandling = NullValueHandling.Ignore)]
    public string? IsolationScope { get; set; }
}

public sealed class ServerFarmProperties
{
    [NodeProperty("status")]
    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public string? Status { get; set; }

    [NodeProperty("maximumNumberOfWorkers")]
    [JsonProperty("maximumNumberOfWorkers", NullValueHandling = NullValueHandling.Ignore)]
    public long? MaximumNumberOfWorkers { get; set; }

    [NodeProperty("numberOfWorkers")]
    [JsonProperty("numberOfWorkers", NullValueHandling = NullValueHandling.Ignore)]
    public long? NumberOfWorkers { get; set; }
}

public sealed class StorageAccount : AzureResourceNode
{
    [NodeProperty("kind")]
    [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kind { get; set; }

    [NodeProperty("")]
    [JsonProperty("sku", NullValueHandling = NullValueHandling.Ignore)]
    public Sku? Sku { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public StorageAccountProperties? Properties { get; set; }

    [NodeProperty("webEndpoint")]
    [JsonIgnore]
    public string? WebEndpoint => Properties?.PrimaryEndpoints?.Web?.ToString();
}

public sealed class StorageAccountProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("primaryLocation")]
    [JsonProperty("primaryLocation", NullValueHandling = NullValueHandling.Ignore)]
    public string? PrimaryLocation { get; set; }

    [NodeProperty("statusOfPrimary")]
    [JsonProperty("statusOfPrimary", NullValueHandling = NullValueHandling.Ignore)]
    public string? StatusOfPrimary { get; set; }

    [NodeProperty("publicNetworkAccess")]
    [JsonProperty("publicNetworkAccess", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicNetworkAccess { get; set; }

    [JsonProperty("primaryEndpoints", NullValueHandling = NullValueHandling.Ignore)]
    public StorageEndpoints? PrimaryEndpoints { get; set; }
}

public sealed class StorageEndpoints
{
    [JsonProperty("blob", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Blob { get; set; }

    [JsonProperty("queue", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Queue { get; set; }

    [JsonProperty("table", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Table { get; set; }

    [JsonProperty("file", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? File { get; set; }

    [JsonProperty("web", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Web { get; set; }
}

public sealed class KeyVault : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public KeyVaultProperties? Properties { get; set; }

    public static string GetKeyVaultName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var withoutProtocol = url.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase);
        return withoutProtocol.Split(".vault.azure.net", StringSplitOptions.RemoveEmptyEntries)[0];
    }
}

public sealed class KeyVaultProperties
{
    [JsonProperty("tenantId", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? TenantId { get; set; }

    [NodeProperty("tenantId")]
    [JsonIgnore]
    public string? TenantIdValue => TenantId?.ToString();

    [NodeProperty("vaultUri")]
    [JsonProperty("vaultUri", NullValueHandling = NullValueHandling.Ignore)]
    public string? VaultUri { get; set; }

    [NodeProperty("enableRbacAuthorization")]
    [JsonProperty("enableRbacAuthorization", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableRbacAuthorization { get; set; }

    [NodeProperty("publicNetworkAccess")]
    [JsonProperty("publicNetworkAccess", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicNetworkAccess { get; set; }
}

public sealed class ContainerRegistry : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("sku", NullValueHandling = NullValueHandling.Ignore)]
    public Sku? Sku { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerRegistryProperties? Properties { get; set; }
}

public sealed class ContainerRegistryProperties
{
    [NodeProperty("loginServer")]
    [JsonProperty("loginServer", NullValueHandling = NullValueHandling.Ignore)]
    public string? LoginServer { get; set; }

    [NodeProperty("adminUserEnabled")]
    [JsonProperty("adminUserEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AdminUserEnabled { get; set; }

    [NodeProperty("publicNetworkAccess")]
    [JsonProperty("publicNetworkAccess", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicNetworkAccess { get; set; }
}

public sealed class CosmosDbAccount : AzureResourceNode
{
    [NodeProperty("kind")]
    [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kind { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public CosmosDbAccountProperties? Properties { get; set; }

    [NodeProperty("documentEndpoint")]
    [JsonIgnore]
    public string? DocumentEndpoint => Properties?.DocumentEndpoint;

    [NodeProperty("databaseAccountOfferType")]
    [JsonIgnore]
    public string? DatabaseAccountOfferType => Properties?.DatabaseAccountOfferType;

    [NodeProperty("provisioningState")]
    [JsonIgnore]
    public string? ProvisioningState => Properties?.ProvisioningState;

    [NodeProperty("publicNetworkAccess")]
    [JsonIgnore]
    public string? PublicNetworkAccess => Properties?.PublicNetworkAccess;

    [NodeProperty("defaultIdentity")]
    [JsonIgnore]
    public string? DefaultIdentity => Properties?.DefaultIdentity;

    [NodeProperty("enableFreeTier")]
    [JsonIgnore]
    public bool? EnableFreeTier => Properties?.EnableFreeTier;

    [NodeProperty("enableAutomaticFailover")]
    [JsonIgnore]
    public bool? EnableAutomaticFailover => Properties?.EnableAutomaticFailover;

    [NodeProperty("enableMultipleWriteLocations")]
    [JsonIgnore]
    public bool? EnableMultipleWriteLocations => Properties?.EnableMultipleWriteLocations;

    [NodeProperty("locations")]
    [JsonIgnore]
    public string? Locations => Properties?.Locations is { Count: > 0 } locations
        ? string.Join(", ", locations.Select(location => location.LocationName).Where(name => !string.IsNullOrWhiteSpace(name)))
        : null;

    [NodeProperty("capabilities")]
    [JsonIgnore]
    public string? Capabilities => Properties?.Capabilities is { Count: > 0 } capabilities
        ? string.Join(", ", capabilities.Select(capability => capability.Name).Where(name => !string.IsNullOrWhiteSpace(name)))
        : null;
}

public sealed class CosmosDbAccountProperties
{
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [JsonProperty("documentEndpoint", NullValueHandling = NullValueHandling.Ignore)]
    public string? DocumentEndpoint { get; set; }

    [JsonProperty("databaseAccountOfferType", NullValueHandling = NullValueHandling.Ignore)]
    public string? DatabaseAccountOfferType { get; set; }

    [JsonProperty("publicNetworkAccess", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicNetworkAccess { get; set; }

    [JsonProperty("defaultIdentity", NullValueHandling = NullValueHandling.Ignore)]
    public string? DefaultIdentity { get; set; }

    [JsonProperty("enableFreeTier", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableFreeTier { get; set; }

    [JsonProperty("enableAutomaticFailover", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableAutomaticFailover { get; set; }

    [JsonProperty("enableMultipleWriteLocations", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableMultipleWriteLocations { get; set; }

    [JsonProperty("locations", NullValueHandling = NullValueHandling.Ignore)]
    public List<CosmosDbAccountLocation> Locations { get; set; } = [];

    [JsonProperty("capabilities", NullValueHandling = NullValueHandling.Ignore)]
    public List<CosmosDbAccountCapability> Capabilities { get; set; } = [];
}

public sealed class CosmosDbAccountLocation
{
    [JsonProperty("locationName", NullValueHandling = NullValueHandling.Ignore)]
    public string? LocationName { get; set; }

    [JsonProperty("failoverPriority", NullValueHandling = NullValueHandling.Ignore)]
    public int? FailoverPriority { get; set; }

    [JsonProperty("isZoneRedundant", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsZoneRedundant { get; set; }
}

public sealed class CosmosDbAccountCapability
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }
}

public sealed class RedisCache : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public RedisProperties? Properties { get; set; }
}

public sealed class RedisProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("redisVersion")]
    [JsonProperty("redisVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? RedisVersion { get; set; }

    [NodeProperty("hostName")]
    [JsonProperty("hostName", NullValueHandling = NullValueHandling.Ignore)]
    public string? HostName { get; set; }

    [NodeProperty("minimumTlsVersion")]
    [JsonProperty("minimumTlsVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? MinimumTlsVersion { get; set; }

    [NodeProperty("publicNetworkAccess")]
    [JsonProperty("publicNetworkAccess", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicNetworkAccess { get; set; }
}

public sealed class SqlManagedInstance : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SqlManagedInstanceProperties? Properties { get; set; }

    [NodeRelationship<Subnet>(Edges.DeployedInSubnet)]
    public List<string> DeployedInSubnets { get; set; } = [];

    public static class Edges
    {
        public const string DeployedInSubnet = "DEPLOYED_IN_SUBNET";
    }
}

public sealed class SqlManagedInstanceProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("fullyQualifiedDomainName")]
    [JsonProperty("fullyQualifiedDomainName", NullValueHandling = NullValueHandling.Ignore)]
    public string? FullyQualifiedDomainName { get; set; }

    [NodeProperty("state")]
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }

    [NodeProperty("vCores")]
    [JsonProperty("vCores", NullValueHandling = NullValueHandling.Ignore)]
    public int? VCores { get; set; }

    [NodeProperty("storageSizeInGB")]
    [JsonProperty("storageSizeInGB", NullValueHandling = NullValueHandling.Ignore)]
    public int? StorageSizeInGB { get; set; }

    [JsonProperty("subnetId", NullValueHandling = NullValueHandling.Ignore)]
    public string? SubnetId { get; set; }
}
