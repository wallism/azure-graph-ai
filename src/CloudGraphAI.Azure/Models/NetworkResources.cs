using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public sealed class VNet : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public VNetProperties? Properties { get; set; }

    [NodeRelationship<Subnet>(Edges.HasSubnet)]
    public List<string> Subnets { get; set; } = [];

    [NodeRelationship<VirtualNetworkPeering>(Edges.HasPeering)]
    public List<string> Peerings { get; set; } = [];

    [NodeProperty("addressPrefixes")]
    [JsonIgnore]
    public string? AddressPrefixes => Properties?.AddressSpace?.AddressPrefixes is { Count: > 0 } prefixes
        ? string.Join(", ", prefixes)
        : null;

    [NodeProperty("dnsServers")]
    [JsonIgnore]
    public string? DnsServers => AzureNetworkPropertyFormatter.Join(Properties?.DhcpOptions?.DnsServers);

    public static class Edges
    {
        public const string HasSubnet = "HAS_SUBNET";
        public const string HasPeering = "HAS_PEERING";
    }
}

public sealed class VNetProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("resourceGuid")]
    [JsonProperty("resourceGuid", NullValueHandling = NullValueHandling.Ignore)]
    public string? ResourceGuid { get; set; }

    [NodeProperty("privateEndpointVNetPolicies")]
    [JsonProperty("privateEndpointVNetPolicies", NullValueHandling = NullValueHandling.Ignore)]
    public string? PrivateEndpointVNetPolicies { get; set; }

    [NodeProperty("enableDdosProtection")]
    [JsonProperty("enableDdosProtection", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnableDdosProtection { get; set; }

    [JsonProperty("dhcpOptions", NullValueHandling = NullValueHandling.Ignore)]
    public DhcpOptions? DhcpOptions { get; set; }

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

public sealed class DhcpOptions
{
    [JsonProperty("dnsServers", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> DnsServers { get; set; } = [];
}

public sealed class Subnet : AzureResourceNode
{
    [NodeProperty("belongsToVNet")]
    [JsonIgnore]
    public string? BelongsToVNet { get; set; }

    [NodeProperty("vnetId")]
    [JsonIgnore]
    public string? VNetId => GetParentVNetId(Id);

    [NodeProperty("networkSecurityGroupId")]
    [JsonIgnore]
    public string? NetworkSecurityGroupId => Properties?.NetworkSecurityGroup?.Id;

    [NodeProperty("routeTableId")]
    [JsonIgnore]
    public string? RouteTableId => Properties?.RouteTable?.Id;

    [NodeProperty("addressPrefixes")]
    [JsonIgnore]
    public string? AddressPrefixes => Properties?.AddressPrefixes is { Count: > 0 } prefixes
        ? string.Join(", ", prefixes)
        : Properties?.AddressPrefix;

    [NodeProperty("serviceEndpointServices")]
    [JsonIgnore]
    public string? ServiceEndpointServices => AzureNetworkPropertyFormatter.Join(
        Properties?.ServiceEndpoints.Select(endpoint => endpoint.Service));

    [NodeProperty("delegationServiceNames")]
    [JsonIgnore]
    public string? DelegationServiceNames => AzureNetworkPropertyFormatter.Join(
        Properties?.Delegations.Select(delegation => delegation.Properties?.ServiceName));

    [NodeProperty("ipConfigurationIds")]
    [JsonIgnore]
    public string? IpConfigurationIds => AzureNetworkPropertyFormatter.Join(
        Properties?.IpConfigurations.Select(reference => reference.Id));

    [NodeProperty("networkIntentPolicyIds")]
    [JsonIgnore]
    public string? NetworkIntentPolicyIds => AzureNetworkPropertyFormatter.Join(
        Properties?.NetworkIntentPolicies.Select(reference => reference.Id));

    [NodeProperty("serviceAssociationLinkIds")]
    [JsonIgnore]
    public string? ServiceAssociationLinkIds => AzureNetworkPropertyFormatter.Join(
        Properties?.ServiceAssociationLinks.Select(link => link.Id));

    [NodeProperty("serviceAssociationLinkedResourceTypes")]
    [JsonIgnore]
    public string? ServiceAssociationLinkedResourceTypes => AzureNetworkPropertyFormatter.Join(
        Properties?.ServiceAssociationLinks.Select(link => link.Properties?.LinkedResourceType));

    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SubnetProperties? Properties { get; set; }

    private static string? GetParentVNetId(string? subnetId)
    {
        if (string.IsNullOrWhiteSpace(subnetId))
            return null;

        var subnetSegment = subnetId.IndexOf("/subnets/", StringComparison.OrdinalIgnoreCase);
        return subnetSegment > 0 ? subnetId[..subnetSegment] : null;
    }
}

public sealed class SubnetProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [NodeProperty("addressPrefix")]
    [JsonProperty("addressPrefix", NullValueHandling = NullValueHandling.Ignore)]
    public string? AddressPrefix { get; set; }

    [JsonProperty("addressPrefixes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> AddressPrefixes { get; set; } = [];

    [JsonProperty("serviceEndpoints", NullValueHandling = NullValueHandling.Ignore)]
    public List<ServiceEndpoint> ServiceEndpoints { get; set; } = [];

    [JsonProperty("delegations", NullValueHandling = NullValueHandling.Ignore)]
    public List<SubnetDelegation> Delegations { get; set; } = [];

    [NodeProperty("privateEndpointNetworkPolicies")]
    [JsonProperty("privateEndpointNetworkPolicies", NullValueHandling = NullValueHandling.Ignore)]
    public string? PrivateEndpointNetworkPolicies { get; set; }

    [NodeProperty("privateLinkServiceNetworkPolicies")]
    [JsonProperty("privateLinkServiceNetworkPolicies", NullValueHandling = NullValueHandling.Ignore)]
    public string? PrivateLinkServiceNetworkPolicies { get; set; }

    [JsonProperty("ipConfigurations", NullValueHandling = NullValueHandling.Ignore)]
    public List<AzureResourceReference> IpConfigurations { get; set; } = [];

    [JsonProperty("networkSecurityGroup", NullValueHandling = NullValueHandling.Ignore)]
    public AzureResourceReference? NetworkSecurityGroup { get; set; }

    [JsonProperty("routeTable", NullValueHandling = NullValueHandling.Ignore)]
    public AzureResourceReference? RouteTable { get; set; }

    [JsonProperty("networkIntentPolicies", NullValueHandling = NullValueHandling.Ignore)]
    public List<AzureResourceReference> NetworkIntentPolicies { get; set; } = [];

    [JsonProperty("serviceAssociationLinks", NullValueHandling = NullValueHandling.Ignore)]
    public List<ServiceAssociationLink> ServiceAssociationLinks { get; set; } = [];
}

public sealed class ServiceEndpoint
{
    [JsonProperty("service", NullValueHandling = NullValueHandling.Ignore)]
    public string? Service { get; set; }

    [JsonProperty("locations", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Locations { get; set; } = [];
}

public sealed class SubnetDelegation
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public SubnetDelegationProperties? Properties { get; set; }
}

public sealed class SubnetDelegationProperties
{
    [JsonProperty("serviceName", NullValueHandling = NullValueHandling.Ignore)]
    public string? ServiceName { get; set; }

    [JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Actions { get; set; } = [];
}

public sealed class ServiceAssociationLink
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public ServiceAssociationLinkProperties? Properties { get; set; }
}

public sealed class ServiceAssociationLinkProperties
{
    [JsonProperty("linkedResourceType", NullValueHandling = NullValueHandling.Ignore)]
    public string? LinkedResourceType { get; set; }

    [JsonProperty("link", NullValueHandling = NullValueHandling.Ignore)]
    public string? Link { get; set; }

    [JsonProperty("enabledForArmDeployments", NullValueHandling = NullValueHandling.Ignore)]
    public bool? EnabledForArmDeployments { get; set; }

    [JsonProperty("allowDelete", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AllowDelete { get; set; }
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

internal static class AzureNetworkPropertyFormatter
{
    public static string? Join(IEnumerable<string?>? values)
    {
        var list = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        return list.Count == 0 ? null : string.Join(", ", list);
    }
}
