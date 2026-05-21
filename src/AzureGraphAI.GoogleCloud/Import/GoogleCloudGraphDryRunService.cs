using AzureGraphAI.GoogleCloud.Api;
using AzureGraphAI.GoogleCloud.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Neo4jLiteRepo.Setup;

namespace AzureGraphAI.GoogleCloud.Import;

public interface IGoogleCloudGraphDryRunService
{
    Task<GoogleCloudGraphDryRunSummary> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record GoogleCloudGraphDryRunSummary(
    bool Neo4jSucceeded,
    string? Neo4jServer,
    string? Neo4jError,
    string? Neo4jConnectionUri,
    string? Neo4jUser,
    string? Neo4jDatabase,
    bool Neo4jPasswordConfigured,
    int Neo4jPasswordLength,
    string AuthenticationMode,
    bool ApiKeyConfigured,
    IReadOnlyList<GoogleCloudScopeDryRunResult> ScopeResults)
{
    public bool Succeeded => Neo4jSucceeded && ScopeResults.All(result => result.Succeeded);
}

public sealed record GoogleCloudScopeDryRunResult(
    string Scope,
    bool Succeeded,
    int AssetCount,
    string? Error);

public sealed class GoogleCloudGraphDryRunService(
    IConfiguration configuration,
    IGoogleCloudRestApi googleCloudApi,
    IDriver neo4jDriver,
    ILogger<GoogleCloudGraphDryRunService> logger)
    : IGoogleCloudGraphDryRunService
{
    public async Task<GoogleCloudGraphDryRunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        var scopes = GoogleCloudGraphScopeConfiguration.LoadScopes(configuration);
        if (scopes.Count == 0)
            throw new InvalidOperationException("No Google Cloud scopes configured. Set GoogleCloudGraph:IncludedScopes, IncludedProjects, IncludedFolders, or IncludedOrganizations.");

        var options = configuration.GetSection("GoogleCloudGraph").Get<GoogleCloudGraphOptions>() ?? new();
        var neo4jSettings = new Neo4jSettings { User = "", Password = "", Database = "" };
        configuration.GetSection("Neo4jSettings").Bind(neo4jSettings);
        var neo4jConnectionUri = neo4jSettings.ConnectionUri?.ToString() ?? configuration["Neo4jSettings:Connection"];
        var neo4jPasswordConfigured = !string.IsNullOrEmpty(neo4jSettings.Password);
        var neo4jPasswordLength = neo4jSettings.Password?.Length ?? 0;

        var neo4jSucceeded = false;
        string? neo4jServer = null;
        string? neo4jError = null;

        try
        {
            logger.LogInformation("Verifying Neo4j connectivity");
            await neo4jDriver.VerifyConnectivityAsync().ConfigureAwait(false);
            var serverInfo = await neo4jDriver.GetServerInfoAsync().ConfigureAwait(false);
            neo4jServer = serverInfo.Address;
            neo4jSucceeded = true;
        }
        catch (Exception ex)
        {
            neo4jError = ex.Message;
            logger.LogError(ex, "Neo4j connectivity check failed");
        }

        var scopeResults = new List<GoogleCloudScopeDryRunResult>();
        foreach (var scope in scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Loading one Google Cloud Asset Inventory page from {Scope}", scope);

            try
            {
                var page = await googleCloudApi.ListAssetsPageAsync(
                    scope,
                    ["cloudresourcemanager.googleapis.com/Project"],
                    1,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                scopeResults.Add(new GoogleCloudScopeDryRunResult(scope, true, page.Assets.Count, null));
            }
            catch (Exception ex)
            {
                scopeResults.Add(new GoogleCloudScopeDryRunResult(scope, false, 0, ex.Message));
                logger.LogError(ex, "Google Cloud scope check failed for {Scope}", scope);
            }
        }

        return new GoogleCloudGraphDryRunSummary(
            neo4jSucceeded,
            neo4jServer,
            neo4jError,
            neo4jConnectionUri,
            neo4jSettings.User,
            neo4jSettings.Database,
            neo4jPasswordConfigured,
            neo4jPasswordLength,
            options.Authentication.Mode,
            !string.IsNullOrWhiteSpace(options.Authentication.ApiKey),
            scopeResults);
    }
}
