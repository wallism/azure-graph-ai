using CloudGraphAI.Azure.Models;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class NetworkResourcesTests
{
    [Test]
    public void Subnet_ExposesGraphFriendlyNetworkProperties()
    {
        var subnet = new Subnet
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/vnet1/subnets/subnet1",
            Name = "subnet1",
            Properties = new SubnetProperties
            {
                AddressPrefix = "10.0.1.0/24",
                AddressPrefixes = ["10.0.1.0/24", "10.0.2.0/24"],
                PrivateEndpointNetworkPolicies = "Disabled",
                PrivateLinkServiceNetworkPolicies = "Enabled",
                NetworkSecurityGroup = new AzureResourceReference
                {
                    Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/networkSecurityGroups/nsg1"
                },
                RouteTable = new AzureResourceReference
                {
                    Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/routeTables/rt1"
                },
                ServiceEndpoints =
                [
                    new ServiceEndpoint { Service = "Microsoft.Storage" },
                    new ServiceEndpoint { Service = "Microsoft.KeyVault" }
                ],
                Delegations =
                [
                    new SubnetDelegation
                    {
                        Properties = new SubnetDelegationProperties
                        {
                            ServiceName = "Microsoft.Web/serverFarms"
                        }
                    }
                ],
                ServiceAssociationLinks =
                [
                    new ServiceAssociationLink
                    {
                        Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/vnet1/subnets/subnet1/serviceAssociationLinks/link1",
                        Properties = new ServiceAssociationLinkProperties
                        {
                            LinkedResourceType = "Microsoft.Sql/managedInstances"
                        }
                    }
                ]
            }
        };

        Assert.That(subnet.VNetId, Is.EqualTo("/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/vnet1"));
        Assert.That(subnet.AddressPrefixes, Is.EqualTo("10.0.1.0/24, 10.0.2.0/24"));
        Assert.That(subnet.NetworkSecurityGroupId, Does.EndWith("/networkSecurityGroups/nsg1"));
        Assert.That(subnet.RouteTableId, Does.EndWith("/routeTables/rt1"));
        Assert.That(subnet.ServiceEndpointServices, Is.EqualTo("Microsoft.Storage, Microsoft.KeyVault"));
        Assert.That(subnet.DelegationServiceNames, Is.EqualTo("Microsoft.Web/serverFarms"));
        Assert.That(subnet.ServiceAssociationLinkIds, Does.EndWith("/serviceAssociationLinks/link1"));
        Assert.That(subnet.ServiceAssociationLinkedResourceTypes, Is.EqualTo("Microsoft.Sql/managedInstances"));
    }

    [Test]
    public void VNet_ExposesDnsServersAsGraphPropertyText()
    {
        var vnet = new VNet
        {
            Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.Network/virtualNetworks/vnet1",
            Name = "vnet1",
            Properties = new VNetProperties
            {
                DhcpOptions = new DhcpOptions
                {
                    DnsServers = ["10.0.0.4", "10.0.0.5"]
                }
            }
        };

        Assert.That(vnet.DnsServers, Is.EqualTo("10.0.0.4, 10.0.0.5"));
    }
}
