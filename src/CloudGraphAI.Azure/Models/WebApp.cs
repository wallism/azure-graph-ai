using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public sealed class WebApp : AzureResourceNode
{
    [NodeProperty("kind")]
    [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
    public string? Kind { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public WebAppProperties? Properties { get; set; }

    [NodeProperty("os")]
    [JsonIgnore]
    public string OperatingSystem => Kind?.Contains("linux", StringComparison.OrdinalIgnoreCase) == true ? "linux" : "windows";

    [NodeProperty("enabledHostNames")]
    [JsonIgnore]
    public string? EnabledHostNames => Properties?.EnabledHostNames is { Count: > 0 }
        ? string.Join(", ", Properties.EnabledHostNames)
        : null;

    [NodeProperty("hasIPSecurityRestrictions")]
    [JsonIgnore]
    public bool HasIPSecurityRestrictions => Properties?.SiteConfig?.IpSecurityRestrictionsDefaultAction is not null;

    [NodeRelationship<ServerFarm>(Edges.RunsOnFarm)]
    public List<string> ServerFarms { get; set; } = [];

    [NodeRelationship<Subnet>(Edges.DeployedInSubnet)]
    public List<string> DeployedInSubnets { get; set; } = [];

    [NodeRelationship<KeyVault>(Edges.ConnectsToKeyVault)]
    public List<string> KeyVaults { get; set; } = [];

    [NodeRelationship<StorageAccount>(Edges.ConnectsToStorage)]
    public List<string> StorageAccounts { get; set; } = [];

    [NodeRelationship<RedisCache>(Edges.ConnectsToRedis)]
    public List<string> RedisCaches { get; set; } = [];

    [NodeRelationship<SqlManagedInstance>(Edges.ConnectsToSqlManagedInstance)]
    public List<string> SqlManagedInstances { get; set; } = [];

    [NodeRelationship<AzureAIFoundryAccount>(Edges.ConnectsToAzureAIFoundry)]
    public List<string> AzureAIFoundryAccounts { get; set; } = [];

    [NodeRelationship<WebAppSiteConfig>(Edges.HasSiteConfig)]
    public List<string> SiteConfigs { get; set; } = [];

    [JsonIgnore]
    public List<ConnectionToService> ConnectionsTo { get; } = [];

    [JsonIgnore]
    public List<string> KeyVaultReferenceCandidates { get; } = [];

    [JsonIgnore]
    public List<string> AzureAIFoundryEndpointCandidates { get; } = [];

    public static class Edges
    {
        public const string RunsOnFarm = "RUNS_ON_FARM";
        public const string DeployedInSubnet = "DEPLOYED_IN_SUBNET";
        public const string ConnectsToKeyVault = "CONNECTS_TO_KEYVAULT";
        public const string ConnectsToStorage = "CONNECTS_TO_STORAGE";
        public const string ConnectsToRedis = "CONNECTS_TO_REDIS";
        public const string ConnectsToSqlManagedInstance = "CONNECTS_TO_SQLMI";
        public const string ConnectsToAzureAIFoundry = "CONNECTS_TO_AI_FOUNDRY";
        public const string HasSiteConfig = "HAS_SITE_CONFIG";
    }
}

public sealed class WebAppProperties
{
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }

    [JsonProperty("enabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Enabled { get; set; }

    [JsonProperty("enabledHostNames", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> EnabledHostNames { get; set; } = [];

    [JsonProperty("hostNameSslStates", NullValueHandling = NullValueHandling.Ignore)]
    public List<HostNameSslState> HostNameSslStates { get; set; } = [];

    [NodeProperty("serverFarmId")]
    [JsonProperty("serverFarmId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ServerFarmId { get; set; }

    [NodeProperty("httpsOnly")]
    [JsonProperty("httpsOnly", NullValueHandling = NullValueHandling.Ignore)]
    public bool? HttpsOnly { get; set; }

    [NodeProperty("inboundIpAddress")]
    [JsonProperty("inboundIpAddress", NullValueHandling = NullValueHandling.Ignore)]
    public string? InboundIpAddress { get; set; }

    [JsonProperty("outboundIpAddresses", NullValueHandling = NullValueHandling.Ignore)]
    public string? OutboundIpAddresses { get; set; }

    [JsonProperty("possibleOutboundIpAddresses", NullValueHandling = NullValueHandling.Ignore)]
    public string? PossibleOutboundIpAddresses { get; set; }

    [JsonProperty("virtualNetworkSubnetId", NullValueHandling = NullValueHandling.Ignore)]
    public string? VirtualNetworkSubnetId { get; set; }

    [JsonProperty("defaultHostName", NullValueHandling = NullValueHandling.Ignore)]
    public string? DefaultHostName { get; set; }

    [JsonIgnore]
    public SiteConfig? SiteConfig { get; set; }
}

public sealed class HostNameSslState
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("sslState", NullValueHandling = NullValueHandling.Ignore)]
    public string? SslState { get; set; }
}

public sealed class WebAppConfig
{
    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public List<SiteConfigWrapper> Value { get; set; } = [];
}

public sealed class SiteConfigWrapper
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public string? Location { get; set; }

    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SiteConfig? Properties { get; set; }
}

public sealed class WebAppSiteConfig : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SiteConfig? Properties { get; set; }
}

public sealed class SiteConfig
{
    [NodeProperty("http20Enabled")]
    [JsonProperty("http20Enabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Http20Enabled { get; set; }

    [NodeProperty("minTlsVersion")]
    [JsonProperty("minTlsVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? MinTlsVersion { get; set; }

    [NodeProperty("scmMinTlsVersion")]
    [JsonProperty("scmMinTlsVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? ScmMinTlsVersion { get; set; }

    [NodeProperty("minTlsCipherSuite")]
    [JsonProperty("minTlsCipherSuite", NullValueHandling = NullValueHandling.Ignore)]
    public string? MinTlsCipherSuite { get; set; }

    [NodeProperty("ftpsState")]
    [JsonProperty("ftpsState", NullValueHandling = NullValueHandling.Ignore)]
    public string? FtpsState { get; set; }

    [NodeProperty("alwaysOn")]
    [JsonProperty("alwaysOn", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AlwaysOn { get; set; }

    [NodeProperty("linuxFxVersion")]
    [JsonProperty("linuxFxVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? LinuxFxVersion { get; set; }

    [NodeProperty("scmType")]
    [JsonProperty("scmType", NullValueHandling = NullValueHandling.Ignore)]
    public string? ScmType { get; set; }

    [NodeProperty("numberOfWorkers")]
    [JsonProperty("numberOfWorkers", NullValueHandling = NullValueHandling.Ignore)]
    public long? NumberOfWorkers { get; set; }

    [NodeProperty("use32BitWorkerProcess")]
    [JsonProperty("use32BitWorkerProcess", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Use32BitWorkerProcess { get; set; }

    [NodeProperty("webSocketsEnabled")]
    [JsonProperty("webSocketsEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? WebSocketsEnabled { get; set; }

    [NodeProperty("remoteDebuggingEnabled")]
    [JsonProperty("remoteDebuggingEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? RemoteDebuggingEnabled { get; set; }

    [NodeProperty("vnetRouteAllEnabled")]
    [JsonProperty("vnetRouteAllEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? VnetRouteAllEnabled { get; set; }

    [NodeProperty("healthCheckPath")]
    [JsonProperty("healthCheckPath", NullValueHandling = NullValueHandling.Ignore)]
    public string? HealthCheckPath { get; set; }

    [NodeProperty("ipSecurityRestrictionsDefaultAction")]
    [JsonProperty("ipSecurityRestrictionsDefaultAction", NullValueHandling = NullValueHandling.Ignore)]
    public string? IpSecurityRestrictionsDefaultAction { get; set; }

    [NodeProperty("scmIpSecurityRestrictionsDefaultAction")]
    [JsonProperty("scmIpSecurityRestrictionsDefaultAction", NullValueHandling = NullValueHandling.Ignore)]
    public string? ScmIpSecurityRestrictionsDefaultAction { get; set; }
}
