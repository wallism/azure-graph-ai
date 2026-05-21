using Agile.API.Clients.CallHandling;
using AzureGraphAI.Core.Azure.Api;
using AzureGraphAI.Core.Azure.Models;
using Microsoft.Extensions.Logging;

namespace AzureGraphAI.Core.Azure.Import;

public abstract class AzureResourceCollectorBase<T>(
    IAzureRestApi azureApi,
    ILogger logger)
    : IAzureResourceCollector
    where T : AzureResourceNode
{
    protected IAzureRestApi AzureApi { get; } = azureApi;

    public virtual int Order => 100;

    public virtual string Name => typeof(T).Name;

    public virtual async Task<IReadOnlyList<AzureGraphNode>> CollectAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        var resources = new List<T>();

        foreach (var subscriptionId in context.SubscriptionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loaded = await LoadAllPagesAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
            foreach (var resource in loaded)
            {
                context.ApplyCommonResourceLinks(resource);
                await EnrichResourceAsync(resource, context, subscriptionId, cancellationToken).ConfigureAwait(false);
            }

            resources.AddRange(loaded);
        }

        context.AddNodes(resources);
        return resources;
    }

    protected abstract Task<CallResult<AzureResourceListResult<T>>> LoadFirstPageAsync(string subscriptionId, CancellationToken cancellationToken);

    protected virtual Task EnrichResourceAsync(T resource, AzureImportContext context, string subscriptionId, CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected async Task<List<T>> LoadAllPagesAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        var firstPage = await LoadFirstPageAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        if (!firstPage.WasSuccessful || firstPage.Value is null)
            throw new InvalidOperationException($"Failed to load {typeof(T).Name} resources for subscription {subscriptionId}: {firstPage.Exception?.Message ?? firstPage.RawText}");

        var all = firstPage.Value.Value;
        var nextLink = firstPage.Value.NextLink;

        while (!string.IsNullOrWhiteSpace(nextLink))
        {
            logger.LogInformation("Loading next page of {ResourceType} resources", typeof(T).Name);
            var nextPage = await AzureApi.GetNextPageAsync<T>(nextLink, cancellationToken).ConfigureAwait(false);
            if (!nextPage.WasSuccessful || nextPage.Value is null)
                throw new InvalidOperationException($"Failed to load next page of {typeof(T).Name}: {nextPage.Exception?.Message ?? nextPage.RawText}");

            all.AddRange(nextPage.Value.Value);
            nextLink = nextPage.Value.NextLink;
        }

        return all;
    }
}
