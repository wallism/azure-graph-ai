using Agile.API.Clients.CallHandling;
using AzureGraphAI.Core.Azure.Api;
using AzureGraphAI.Core.Azure.Models;
using Microsoft.Extensions.Logging;

namespace AzureGraphAI.Core.Azure.Import;

public sealed class ContainerAppCollector(
    IAzureRestApi azureApi,
    ILogger<ContainerAppCollector> logger)
    : AzureResourceCollectorBase<ContainerApp>(azureApi, logger)
{
    public override int Order => 60;

    protected override Task<CallResult<AzureResourceListResult<ContainerApp>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetContainerAppsAsync(subscriptionId, cancellationToken);

    public Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var app in context.GetNodes<ContainerApp>())
        {
            var registryNames = app.Properties?.Configuration?.Registries
                .Select(registry => RegistryServerToName(registry.Server))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            foreach (var registryName in registryNames)
            {
                var registry = context.Find<ContainerRegistry>(candidate =>
                    candidate.Name?.Equals(registryName, StringComparison.OrdinalIgnoreCase) == true ||
                    candidate.Properties?.LoginServer?.StartsWith($"{registryName}.", StringComparison.OrdinalIgnoreCase) == true);

                if (registry is not null)
                    app.PullsFromRegistries.Add(registry.Id);
            }
        }

        return Task.CompletedTask;
    }

    private static string? RegistryServerToName(string? server)
    {
        if (string.IsNullOrWhiteSpace(server))
            return null;

        return server.Replace(".azurecr.io", "", StringComparison.OrdinalIgnoreCase);
    }
}
