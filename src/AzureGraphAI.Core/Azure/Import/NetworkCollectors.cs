using Agile.API.Clients.CallHandling;
using AzureGraphAI.Core.Azure.Api;
using AzureGraphAI.Core.Azure.Models;
using Microsoft.Extensions.Logging;

namespace AzureGraphAI.Core.Azure.Import;

public sealed class VNetCollector(
    IAzureRestApi azureApi,
    ILogger<VNetCollector> logger)
    : AzureResourceCollectorBase<VNet>(azureApi, logger)
{
    public override int Order => 30;

    protected override Task<CallResult<AzureResourceListResult<VNet>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetVNetsAsync(subscriptionId, cancellationToken);

    public override async Task<IReadOnlyList<AzureGraphNode>> CollectAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        var collected = (await base.CollectAsync(context, cancellationToken).ConfigureAwait(false)).OfType<VNet>().ToList();
        var subnets = new List<Subnet>();
        var peerings = new List<VirtualNetworkPeering>();

        foreach (var vnet in collected)
        {
            foreach (var subnet in vnet.Properties?.Subnets ?? [])
            {
                subnet.Location ??= vnet.Location;
                subnet.BelongsToVNet = vnet.Name;
                context.ApplyCommonResourceLinks(subnet);
                vnet.Subnets.Add(subnet.Id);
                subnets.Add(subnet);
            }

            foreach (var peering in vnet.Properties?.VirtualNetworkPeerings ?? [])
            {
                peering.Location ??= vnet.Location;
                context.ApplyCommonResourceLinks(peering);
                vnet.Peerings.Add(peering.Id);
                peerings.Add(peering);
            }
        }

        context.AddNodes(subnets);
        context.AddNodes(peerings);
        return collected.Cast<AzureGraphNode>().Concat(subnets).Concat(peerings).ToList();
    }
}
