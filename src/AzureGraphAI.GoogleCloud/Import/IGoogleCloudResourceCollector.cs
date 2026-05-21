using AzureGraphAI.GoogleCloud.Models;

namespace AzureGraphAI.GoogleCloud.Import;

public interface IGoogleCloudResourceCollector
{
    int Order { get; }

    string Name { get; }

    Task<IReadOnlyList<GoogleCloudGraphNode>> CollectAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default);

    Task BuildRelationshipsAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
