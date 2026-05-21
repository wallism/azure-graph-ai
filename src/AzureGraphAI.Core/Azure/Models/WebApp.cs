using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

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

    [NodeRelationship<ServerFarm>("RUNS_ON_FARM")]
    public List<string> ServerFarms { get; set; } = [];

    [NodeRelationship<Subnet>("DEPLOYED_IN_SUBNET")]
    public List<string> DeployedInSubnets { get; set; } = [];

    [NodeRelationship<KeyVault>("CONNECTS_TO_KEYVAULT")]
    public List<string> KeyVaults { get; set; } = [];

    [NodeRelationship<StorageAccount>("CONNECTS_TO_STORAGE")]
    public List<string> StorageAccounts { get; set; } = [];

    [NodeRelationship<RedisCache>("CONNECTS_TO_REDIS")]
    public List<string> RedisCaches { get; set; } = [];

    [NodeRelationship<SqlManagedInstance>("CONNECTS_TO_SQLMI")]
    public List<string> SqlManagedInstances { get; set; } = [];

    [JsonIgnore]
    public List<ConnectionToService> ConnectionsTo { get; } = [];
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

    [NodeProperty("alwaysOn")]
    [JsonProperty("alwaysOn", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AlwaysOn { get; set; }

    [NodeProperty("linuxFxVersion")]
    [JsonProperty("linuxFxVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? LinuxFxVersion { get; set; }

    [NodeProperty("scmType")]
    [JsonProperty("scmType", NullValueHandling = NullValueHandling.Ignore)]
    public string? ScmType { get; set; }

    [JsonProperty("ipSecurityRestrictionsDefaultAction", NullValueHandling = NullValueHandling.Ignore)]
    public string? IpSecurityRestrictionsDefaultAction { get; set; }
}
