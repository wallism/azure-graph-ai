using Agile.API.Clients.CallHandling;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Models;
using CloudGraphAI.Azure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudGraphAI.Azure.Import;

public sealed class WebAppCollector(
    IAzureRestApi azureApi,
    IConfiguration configuration,
    ILogger<WebAppCollector> logger)
    : AzureResourceCollectorBase<WebApp>(azureApi, logger)
{
    private readonly AzureGraphOptions _options = configuration.GetSection("AzureGraph").Get<AzureGraphOptions>() ?? new AzureGraphOptions();

    public override int Order => 70;

    protected override Task<CallResult<AzureResourceListResult<WebApp>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken)
        => AzureApi.GetWebAppsAsync(subscriptionId, cancellationToken);

    protected override async Task EnrichResourceAsync(WebApp webApp, AzureImportContext context, string subscriptionId, CancellationToken cancellationToken)
    {
        if (!_options.IncludeWebAppDetails)
            return;

        await LoadSiteConfigAsync(webApp, context, subscriptionId, cancellationToken).ConfigureAwait(false);
        await LoadAppSettingsAsync(webApp, subscriptionId, cancellationToken).ConfigureAwait(false);
        await LoadConnectionStringsAsync(webApp, subscriptionId, cancellationToken).ConfigureAwait(false);

        if (_options.IncludeWebJobs && webApp.OperatingSystem.Equals("windows", StringComparison.OrdinalIgnoreCase))
            await LoadWebJobsAsync(webApp, context, subscriptionId, cancellationToken).ConfigureAwait(false);
    }

    public override Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var app in context.GetNodes<WebApp>())
        {
            if (!string.IsNullOrWhiteSpace(app.Properties?.ServerFarmId))
                app.ServerFarms = [app.Properties.ServerFarmId];

            if (!string.IsNullOrWhiteSpace(app.Properties?.VirtualNetworkSubnetId))
                app.DeployedInSubnets = [app.Properties.VirtualNetworkSubnetId];

            AzureAIFoundryEndpointMatcher.AddMatchingAccounts(
                app.AzureAIFoundryAccounts,
                context.GetNodes<AzureAIFoundryAccount>(),
                app.AzureAIFoundryEndpointCandidates);

            KeyVaultReferenceMatcher.AddMatchingVaults(
                app.KeyVaults,
                context.GetNodes<KeyVault>(),
                app.KeyVaultReferenceCandidates);

            foreach (var connection in app.ConnectionsTo)
            {
                switch (connection.Type)
                {
                    case AzureConnectedServiceType.StorageAccount:
                        AddIfFound(app.StorageAccounts, context.FindByName<StorageAccount>(connection.Name));
                        break;
                    case AzureConnectedServiceType.RedisCache:
                        AddIfFound(app.RedisCaches, context.Find<RedisCache>(redis =>
                            redis.Name?.Equals(connection.Name, StringComparison.OrdinalIgnoreCase) == true ||
                            redis.Properties?.HostName?.StartsWith($"{connection.Name}.", StringComparison.OrdinalIgnoreCase) == true));
                        break;
                    case AzureConnectedServiceType.SqlServer:
                        AddIfFound(app.SqlManagedInstances, context.Find<SqlManagedInstance>(sql =>
                            sql.Name?.Equals(connection.Name, StringComparison.OrdinalIgnoreCase) == true ||
                            sql.Properties?.FullyQualifiedDomainName?.StartsWith($"{connection.Name}.", StringComparison.OrdinalIgnoreCase) == true));
                        break;
                    case AzureConnectedServiceType.KeyVault:
                        KeyVaultReferenceMatcher.AddMatchingVaults(
                            app.KeyVaults,
                            context.GetNodes<KeyVault>(),
                            [connection.Name]);
                        break;
                }
            }
        }

        return Task.CompletedTask;
    }

    private static void AddIfFound<T>(ICollection<string> ids, T? node)
        where T : AzureGraphNode
    {
        if (node is not null && !ids.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
            ids.Add(node.Id);
    }

    private async Task LoadSiteConfigAsync(WebApp webApp, AzureImportContext context, string subscriptionId, CancellationToken cancellationToken)
    {
        var result = await AzureApi.GetWebAppSiteConfigAsync(subscriptionId, webApp, cancellationToken).ConfigureAwait(false);
        if (result is not { WasSuccessful: true, Value: not null })
            return;

        var wrapper = result.Value.Value.FirstOrDefault(config => config.Properties is not null);
        if (wrapper?.Properties is null)
            return;

        webApp.Properties ??= new WebAppProperties();
        webApp.Properties.SiteConfig = wrapper.Properties;

        var configNode = new WebAppSiteConfig
        {
            Id = string.IsNullOrWhiteSpace(wrapper.Id) ? $"{webApp.Id}/config/web" : wrapper.Id,
            Name = string.IsNullOrWhiteSpace(wrapper.Name) ? $"{webApp.Name}/web" : wrapper.Name,
            Type = string.IsNullOrWhiteSpace(wrapper.Type) ? "Microsoft.Web/sites/config" : wrapper.Type,
            Location = string.IsNullOrWhiteSpace(wrapper.Location) ? webApp.Location : wrapper.Location,
            Properties = wrapper.Properties
        };
        context.ApplyCommonResourceLinks(configNode);
        context.AddNodes([configNode]);

        if (!webApp.SiteConfigs.Contains(configNode.Id, StringComparer.OrdinalIgnoreCase))
            webApp.SiteConfigs.Add(configNode.Id);
    }

    private async Task LoadAppSettingsAsync(WebApp webApp, string subscriptionId, CancellationToken cancellationToken)
    {
        var result = await AzureApi.GetWebAppSettingsAsync(subscriptionId, webApp, cancellationToken).ConfigureAwait(false);
        if (result is not { WasSuccessful: true, Value: not null })
            return;

        foreach (var keyVaultReference in result.Value.Properties.GetSettingValues(_options.KeyVaultReferenceSettingNames))
        {
            if (!webApp.KeyVaultReferenceCandidates.Contains(keyVaultReference, StringComparer.OrdinalIgnoreCase))
                webApp.KeyVaultReferenceCandidates.Add(keyVaultReference);
        }

        foreach (var endpoint in result.Value.Properties.GetSettingValues(_options.AzureAIFoundryEndpointSettingNames))
        {
            if (!webApp.AzureAIFoundryEndpointCandidates.Contains(endpoint, StringComparer.OrdinalIgnoreCase))
                webApp.AzureAIFoundryEndpointCandidates.Add(endpoint);
        }
    }

    private async Task LoadConnectionStringsAsync(WebApp webApp, string subscriptionId, CancellationToken cancellationToken)
    {
        var result = await AzureApi.GetWebAppConnectionStringsAsync(subscriptionId, webApp, cancellationToken).ConfigureAwait(false);
        if (result is not { WasSuccessful: true, Value: not null })
            return;

        foreach (var connection in result.Value.Properties.Values.Select(value => value.ConnectionToService).OfType<ConnectionToService>())
        {
            if (webApp.ConnectionsTo.All(existing => !existing.Name.Equals(connection.Name, StringComparison.OrdinalIgnoreCase)))
                webApp.ConnectionsTo.Add(connection);
        }
    }

    private async Task LoadWebJobsAsync(WebApp webApp, AzureImportContext context, string subscriptionId, CancellationToken cancellationToken)
    {
        var result = await AzureApi.GetWebAppContinuousWebJobsAsync(subscriptionId, webApp, cancellationToken).ConfigureAwait(false);
        if (result is not { WasSuccessful: true, Value: not null })
            return;

        var webJobs = result.Value.Value
            .Where(webJob => !IsSystemWebJob(webJob))
            .ToList();

        foreach (var webJob in webJobs)
        {
            webJob.Id = string.IsNullOrWhiteSpace(webJob.Id)
                ? $"{webApp.Id}/continuouswebjobs/{webJob.Properties?.Name ?? webJob.Name}"
                : webJob.Id;
            webJob.Name ??= webJob.Properties?.Name;
            webJob.Location ??= webApp.Location;
            webJob.Type ??= "Microsoft.Web/sites/continuouswebjobs";
            webJob.WebApps = [webApp.Id];
            context.ApplyCommonResourceLinks(webJob);
        }

        context.AddNodes(webJobs);
    }

    private static bool IsSystemWebJob(WebJob webJob)
    {
        var name = webJob.Properties?.Name ?? webJob.Name ?? string.Empty;
        return name.Equals("DaaS", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ApplicationInsightsProfiler", StringComparison.OrdinalIgnoreCase);
    }
}
