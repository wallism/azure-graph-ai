using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Options;

namespace CloudGraphAI.Azure.Import;

public sealed class DisplayNameFormatter(IOptions<DisplayNameOptions> options) : IDisplayNameFormatter
{
    private static readonly char[] Separators = ['.', '-', '_'];

    private readonly DisplayNameOptions _options = options.Value;

    public string Format(string rawName, AzureGraphNode node)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return rawName;

        var result = rawName;

        // 1. Strip environment prefix if the node has a resolved environment
        if (node is IEnvironmentAnnotatedResource { Environment: { } env } && _options.StripEnvironmentPrefix)
        {
            result = RemovePrefix(result, env);
        }

        // 2. Strip configured prefixes in order
        foreach (var prefix in _options.PrefixesToRemove)
        {
            result = RemovePrefix(result, prefix);
        }

        // 3. Remove configured substrings
        foreach (var substring in _options.SubstringsToRemove)
        {
            result = result.Replace(substring, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // 4. Trim any leftover separators
        result = result.Trim(Separators).Trim();

        return string.IsNullOrWhiteSpace(result) ? rawName : result;
    }

    private static string RemovePrefix(string name, string prefix)
    {
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return name;

        var remaining = name[prefix.Length..];

        // If a separator follows the prefix, strip it too
        if (remaining.Length > 0 && Separators.Contains(remaining[0]))
            remaining = remaining[1..];

        return remaining.Length == 0 ? name : remaining;
    }
}
