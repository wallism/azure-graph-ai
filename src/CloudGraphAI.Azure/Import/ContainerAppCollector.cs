using Agile.API.Clients.CallHandling;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Models;
using CloudGraphAI.Azure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudGraphAI.Azure.Import;

public sealed class ContainerAppCollector(
    IAzureRestApi azureApi,
    IConfiguration configuration,
    ILogger<ContainerAppCollector> logger)
    : AzureResourceCollectorBase<ContainerApp>(azureApi, logger)
{
    private readonly AzureGraphOptions _options = configuration.GetSection("AzureGraph").Get<AzureGraphOptions>() ?? new AzureGraphOptions();

    public override int Order => 60;

    protected override Task<CallResult<AzureResourceListResult<ContainerApp>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetContainerAppsAsync(subscriptionId, cancellationToken);

    public override Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
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

            AzureAIFoundryEndpointMatcher.AddMatchingAccounts(
                app.AzureAIFoundryAccounts,
                context.GetNodes<AzureAIFoundryAccount>(),
                GetAzureAIFoundryEndpointCandidates(app));
        }

        return Task.CompletedTask;
    }

    private IEnumerable<string> GetAzureAIFoundryEndpointCandidates(ContainerApp app)
    {
        var possibleNames = _options.AzureAIFoundryEndpointSettingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return app.Properties?.Template?.Containers
            .SelectMany(container => container.Env)
            .Where(env => !string.IsNullOrWhiteSpace(env.Name)
                && !string.IsNullOrWhiteSpace(env.Value)
                && possibleNames.Contains(env.Name))
            .Select(env => env.Value!)
            .Distinct(StringComparer.OrdinalIgnoreCase) ?? [];
    }

    private static string? RegistryServerToName(string? server)
    {
        if (string.IsNullOrWhiteSpace(server))
            return null;

        return server.Replace(".azurecr.io", "", StringComparison.OrdinalIgnoreCase);
    }
}
