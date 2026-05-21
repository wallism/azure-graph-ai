using AzureGraphAI.Core.Azure.Models;

namespace AzureGraphAI.Core.Azure.Import;

public interface IAzureResourceCollector
{
    int Order { get; }

    string Name { get; }

    Task<IReadOnlyList<AzureGraphNode>> CollectAsync(AzureImportContext context, CancellationToken cancellationToken = default);

    Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
