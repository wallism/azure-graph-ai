using CloudGraphAI.Azure.Models;

namespace CloudGraphAI.Azure.Import;

internal static class AzureAIFoundryEndpointMatcher
{
    public static void AddMatchingAccounts(
        ICollection<string> relationshipIds,
        IEnumerable<AzureAIFoundryAccount> accounts,
        IEnumerable<string> endpointCandidates)
    {
        var normalizedCandidates = endpointCandidates
            .Select(NormalizeEndpoint)
            .Where(endpoint => !string.IsNullOrWhiteSpace(endpoint))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedCandidates.Count == 0)
            return;

        foreach (var account in accounts)
        {
            var accountEndpoint = NormalizeEndpoint(account.BoundaryApiEndpoint ?? account.Endpoint);
            if (!string.IsNullOrWhiteSpace(accountEndpoint)
                && normalizedCandidates.Contains(accountEndpoint)
                && !relationshipIds.Contains(account.Id, StringComparer.OrdinalIgnoreCase))
            {
                relationshipIds.Add(account.Id);
            }
        }
    }

    private static string? NormalizeEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return null;

        var trimmed = endpoint.Trim().Trim('"', '\'').TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed.ToLowerInvariant();

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant()
        };

        return builder.Uri.ToString().TrimEnd('/').ToLowerInvariant();
    }
}
