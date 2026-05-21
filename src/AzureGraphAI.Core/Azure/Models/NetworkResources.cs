using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class VNet : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public VNetProperties? Properties { get; set; }

    [NodeRelationship<Subnet>("HAS_SUBNET")]
    public List<string> Subnets { get; set; } = [];

    [NodeRelationship<VirtualNetworkPeering>("HAS_PEERING")]
    public List<string> Peerings { get; set; } = [];

    [NodeProperty("addressPrefixes")]
    [JsonIgnore]
    public string? AddressPrefixes => Properties?.AddressSpace?.AddressPrefixes is { Count: > 0 } prefixes
        ? string.Join(", ", prefixes)
        : null;
}

public sealed class VNetProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [JsonProperty("addressSpace", NullValueHandling = NullValueHandling.Ignore)]
    public AddressSpace? AddressSpace { get; set; }

    [JsonProperty("subnets", NullValueHandling = NullValueHandling.Ignore)]
    public List<Subnet> Subnets { get; set; } = [];

    [JsonProperty("virtualNetworkPeerings", NullValueHandling = NullValueHandling.Ignore)]
    public List<VirtualNetworkPeering> VirtualNetworkPeerings { get; set; } = [];
}

public sealed class AddressSpace
{
    [JsonProperty("addressPrefixes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> AddressPrefixes { get; set; } = [];
}

public sealed class Subnet : AzureResourceNode
{
    [NodeProperty("belongsToVNet")]
    [JsonIgnore]
    public string? BelongsToVNet { get; set; }

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SubnetProperties? Properties { get; set; }
}

public sealed class SubnetProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("addressPrefix")]
    [JsonProperty("addressPrefix", NullValueHandling = NullValueHandling.Ignore)]
    public string? AddressPrefix { get; set; }

    [JsonProperty("networkSecurityGroup", NullValueHandling = NullValueHandling.Ignore)]
    public AzureResourceReference? NetworkSecurityGroup { get; set; }

    [JsonProperty("routeTable", NullValueHandling = NullValueHandling.Ignore)]
    public AzureResourceReference? RouteTable { get; set; }
}

public sealed class VirtualNetworkPeering : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public VirtualNetworkPeeringProperties? Properties { get; set; }

    [NodeProperty("remoteVNetId")]
    [JsonIgnore]
    public string? RemoteVNetId => Properties?.RemoteVirtualNetwork?.Id;

    [NodeProperty("remoteVNetName")]
    [JsonIgnore]
    public string? RemoteVNetName => AzureResourceId.GetLastSegment(RemoteVNetId);
}

public sealed class VirtualNetworkPeeringProperties
{
    [NodeProperty("peeringState")]
    [JsonProperty("peeringState", NullValueHandling = NullValueHandling.Ignore)]
    public string? PeeringState { get; set; }

    [NodeProperty("peeringSyncLevel")]
    [JsonProperty("peeringSyncLevel", NullValueHandling = NullValueHandling.Ignore)]
    public string? PeeringSyncLevel { get; set; }

    [JsonProperty("remoteVirtualNetwork", NullValueHandling = NullValueHandling.Ignore)]
    public AzureResourceReference? RemoteVirtualNetwork { get; set; }

    [JsonProperty("remoteAddressSpace", NullValueHandling = NullValueHandling.Ignore)]
    public AddressSpace? RemoteAddressSpace { get; set; }
}

public sealed class AzureResourceReference
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }
}
