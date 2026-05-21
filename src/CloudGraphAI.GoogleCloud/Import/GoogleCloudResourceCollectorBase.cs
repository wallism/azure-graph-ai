using CloudGraphAI.GoogleCloud.Api;
using CloudGraphAI.GoogleCloud.Configuration;
using CloudGraphAI.GoogleCloud.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudGraphAI.GoogleCloud.Import;

public abstract class GoogleCloudResourceCollectorBase<T>(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger logger)
    : IGoogleCloudResourceCollector
    where T : GoogleCloudGraphNode
{
    private readonly GoogleCloudGraphOptions _options = configuration.GetSection("GoogleCloudGraph").Get<GoogleCloudGraphOptions>() ?? new();

    protected IGoogleCloudRestApi GoogleCloudApi { get; } = googleCloudApi;

    public virtual int Order => 100;

    public virtual string Name => typeof(T).Name;

    protected abstract IReadOnlyList<string> AssetTypes { get; }

    protected abstract T MapAsset(GoogleCloudAsset asset);

    public virtual async Task<IReadOnlyList<GoogleCloudGraphNode>> CollectAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
    {
        var resources = new List<T>();

        foreach (var scope in context.Scopes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assets = await LoadAllPagesAsync(scope, cancellationToken).ConfigureAwait(false);
            foreach (var asset in assets)
            {
                var resource = MapAsset(asset);
                if (resource is GoogleCloudResourceNode resourceNode)
                    context.ApplyCommonResourceLinks(resourceNode, asset);
                else
                    context.ApplyHierarchyLinks(resource, asset);

                await EnrichResourceAsync(resource, asset, context, cancellationToken).ConfigureAwait(false);
                resources.Add(resource);
            }
        }

        context.AddNodes(resources);
        return resources.Cast<GoogleCloudGraphNode>().ToList();
    }

    protected virtual Task EnrichResourceAsync(T resource, GoogleCloudAsset asset, GoogleCloudImportContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected async Task<List<GoogleCloudAsset>> LoadAllPagesAsync(string scope, CancellationToken cancellationToken)
    {
        var all = new List<GoogleCloudAsset>();
        string? pageToken = null;

        do
        {
            var page = await GoogleCloudApi.ListAssetsPageAsync(
                scope,
                AssetTypes,
                _options.PageSize,
                pageToken,
                cancellationToken).ConfigureAwait(false);

            all.AddRange(page.Assets.Where(asset => !string.IsNullOrWhiteSpace(asset.Name)));
            pageToken = page.NextPageToken;

            if (!string.IsNullOrWhiteSpace(pageToken))
                logger.LogInformation("Loading next page of {ResourceType} assets from {Scope}", typeof(T).Name, scope);
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return all;
    }
}
