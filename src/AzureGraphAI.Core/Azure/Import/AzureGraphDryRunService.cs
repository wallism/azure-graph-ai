using AzureGraphAI.Core.Azure.Api;
using AzureGraphAI.Core.Azure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace AzureGraphAI.Core.Azure.Import;

public interface IAzureGraphDryRunService
{
    Task<AzureGraphDryRunSummary> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record AzureGraphDryRunSummary(
    string Neo4jServer,
    IReadOnlyList<Subscription> Subscriptions);

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
            throw new InvalidOperationException("No subscriptions configured. Set AzureGraph:IncludedSubscriptions or Azure:IncludedSubscriptions.");

        logger.LogInformation("Verifying Neo4j connectivity");
        await neo4jDriver.VerifyConnectivityAsync().ConfigureAwait(false);
        var serverInfo = await neo4jDriver.GetServerInfoAsync().ConfigureAwait(false);

        var subscriptions = new List<Subscription>();
        foreach (var subscriptionId in subscriptionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Loading Azure subscription {SubscriptionId}", subscriptionId);

            var result = await azureApi.GetSubscriptionAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
            if (!result.WasSuccessful || result.Value is null)
                throw new InvalidOperationException($"Failed to load subscription {subscriptionId}: {result.Exception?.Message ?? result.RawText}");

            var subscription = result.Value;
            subscription.AzureSubscriptionId ??= subscriptionId;
            subscription.Id = AzureResourceId.BuildSubscriptionId(subscriptionId);
            subscription.Type ??= "Microsoft.Resources/subscriptions";
            subscription.Name ??= subscription.AzureDisplayName ?? subscriptionId;
            subscriptions.Add(subscription);
        }

        return new AzureGraphDryRunSummary(serverInfo.Address, subscriptions);
    }
}
