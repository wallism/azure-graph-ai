using AzureGraphAI.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace AzureGraphAI.Core.Azure.Import;

internal static class AzureGraphSubscriptionConfiguration
{
    public static List<string> LoadSubscriptionIds(IConfiguration configuration)
    {
        var options = configuration.GetSection("AzureGraph").Get<AzureGraphOptions>() ?? new AzureGraphOptions();
        var configured = options.IncludedSubscriptions.Count > 0
            ? options.IncludedSubscriptions
            : configuration.GetSection("Azure:IncludedSubscriptions").Get<List<string>>() ?? [];

        return configured
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
