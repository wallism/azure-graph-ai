using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Neo4jLiteRepo.Setup;

namespace CloudGraphAI.Azure.Import;

public interface IAzureGraphDryRunService
{
    Task<AzureGraphDryRunSummary> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record AzureGraphDryRunSummary(
    bool Neo4jSucceeded,
    string? Neo4jServer,
    string? Neo4jError,
    string? Neo4jConnectionUri,
    string? Neo4jUser,
    string? Neo4jDatabase,
    bool Neo4jPasswordConfigured,
    int Neo4jPasswordLength,
    IReadOnlyList<AzureSubscriptionDryRunResult> SubscriptionResults)
{
    public bool Succeeded => Neo4jSucceeded && SubscriptionResults.All(result => result.Succeeded);
}

public sealed record AzureSubscriptionDryRunResult(
    string SubscriptionId,
    bool Succeeded,
    Subscription? Subscription,
    string? Error);

public sealed class AzureGraphDryRunService(
    IConfiguration configuration,
    IAzureRestApi azureApi,
    IDriver neo4jDriver,
    ILogger<AzureGraphDryRunService> logger)
    : IAzureGraphDryRunService
{
    public async Task<AzureGraphDryRunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        var subscriptionIds = AzureGraphSubscriptionConfiguration.LoadSubscriptionIds(configuration);
        if (subscriptionIds.Count == 0)
            throw new InvalidOperationException("No subscriptions configured. Set AzureGraph:IncludedSubscriptions.");

        var neo4jSettings = new Neo4jSettings { User = "", Password = "", Database = "" };
        configuration.GetSection("Neo4jSettings").Bind(neo4jSettings);
        var neo4jConnectionUri = neo4jSettings.ConnectionUri?.ToString() ?? configuration["Neo4jSettings:Connection"];
        var neo4jPasswordConfigured = !string.IsNullOrEmpty(neo4jSettings.Password);
        var neo4jPasswordLength = neo4jSettings.Password?.Length ?? 0;

        logger.LogInformation(
            "Neo4j settings: Uri={Uri}, User={User}, Database={Database}, PasswordConfigured={PasswordConfigured}, PasswordLength={PasswordLength}",
            neo4jConnectionUri,
            neo4jSettings.User,
            neo4jSettings.Database,
            neo4jPasswordConfigured,
            neo4jPasswordLength);

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

        var subscriptionResults = new List<AzureSubscriptionDryRunResult>();
        foreach (var subscriptionId in subscriptionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Loading Azure subscription {SubscriptionId}", subscriptionId);

            try
            {
                var result = await azureApi.GetSubscriptionAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                if (!result.WasSuccessful || result.Value is null)
                {
                    subscriptionResults.Add(new AzureSubscriptionDryRunResult(
                        subscriptionId,
                        false,
                        null,
                        result.Exception?.Message ?? result.RawText ?? "Azure subscription request failed."));
                    continue;
                }

                var subscription = result.Value;
                subscription.AzureSubscriptionId ??= subscriptionId;
                subscription.Id = AzureResourceId.BuildSubscriptionId(subscriptionId);
                subscription.Type ??= "Microsoft.Resources/subscriptions";
                subscription.Name ??= subscription.AzureDisplayName ?? subscriptionId;
                subscriptionResults.Add(new AzureSubscriptionDryRunResult(subscriptionId, true, subscription, null));
            }
            catch (Exception ex)
            {
                subscriptionResults.Add(new AzureSubscriptionDryRunResult(subscriptionId, false, null, ex.Message));
                logger.LogError(ex, "Azure subscription check failed for {SubscriptionId}", subscriptionId);
            }
        }

        return new AzureGraphDryRunSummary(
            neo4jSucceeded,
            neo4jServer,
            neo4jError,
            neo4jConnectionUri,
            neo4jSettings.User,
            neo4jSettings.Database,
            neo4jPasswordConfigured,
            neo4jPasswordLength,
            subscriptionResults);
    }
}
