using CloudGraphAI.Azure.Configuration;
using Microsoft.Extensions.Configuration;

namespace CloudGraphAI.Azure.Import;

internal static class AzureGraphSubscriptionConfiguration
{
    public static List<string> LoadSubscriptionIds(IConfiguration configuration)
    {
        var options = configuration.GetSection("AzureGraph").Get<AzureGraphOptions>() ?? new AzureGraphOptions();
        var configured = options.IncludedSubscriptions;

        return configured
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
