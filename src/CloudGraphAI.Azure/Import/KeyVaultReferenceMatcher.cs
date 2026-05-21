using CloudGraphAI.Azure.Models;

namespace CloudGraphAI.Azure.Import;

internal static class KeyVaultReferenceMatcher
{
    public static void AddMatchingVaults(
        ICollection<string> relationshipIds,
        IEnumerable<KeyVault> vaults,
        IEnumerable<string> referenceCandidates)
    {
        var normalizedCandidates = referenceCandidates
            .SelectMany(BuildCandidateKeys)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedCandidates.Count == 0)
            return;

        foreach (var vault in vaults)
        {
            var vaultKeys = BuildVaultKeys(vault);
            if (vaultKeys.Any(normalizedCandidates.Contains)
                && !relationshipIds.Contains(vault.Id, StringComparer.OrdinalIgnoreCase))
            {
                relationshipIds.Add(vault.Id);
            }
        }
    }

    private static IEnumerable<string> BuildVaultKeys(KeyVault vault)
    {
        if (!string.IsNullOrWhiteSpace(vault.Name))
            yield return vault.Name;

        foreach (var key in BuildCandidateKeys(vault.Properties?.VaultUri))
            yield return key;
    }

    private static IEnumerable<string> BuildCandidateKeys(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        var trimmed = value.Trim().Trim('"', '\'').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(trimmed))
            yield break;

        yield return trimmed;

        var vaultName = KeyVault.GetKeyVaultName(trimmed);
        if (!string.IsNullOrWhiteSpace(vaultName))
            yield return vaultName;

        var normalizedUri = NormalizeUri(trimmed);
        if (!string.IsNullOrWhiteSpace(normalizedUri))
            yield return normalizedUri;
    }

    private static string? NormalizeUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return null;

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant()
        };

        return builder.Uri.ToString().TrimEnd('/');
    }
}
