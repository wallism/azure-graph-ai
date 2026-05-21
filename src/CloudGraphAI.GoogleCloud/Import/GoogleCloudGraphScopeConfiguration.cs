using Microsoft.Extensions.Configuration;

namespace CloudGraphAI.GoogleCloud.Import;

public static class GoogleCloudGraphScopeConfiguration
{
    public static List<string> LoadScopes(IConfiguration configuration)
    {
        var scopes = new List<string>();

        AddConfiguredValues(scopes, configuration.GetSection("GoogleCloudGraph:IncludedScopes"));
        AddConfiguredValues(scopes, configuration.GetSection("GoogleCloud:IncludedScopes"));
        AddPrefixedValues(scopes, configuration.GetSection("GoogleCloudGraph:IncludedProjects"), "projects");
        AddPrefixedValues(scopes, configuration.GetSection("GoogleCloudGraph:IncludedFolders"), "folders");
        AddPrefixedValues(scopes, configuration.GetSection("GoogleCloudGraph:IncludedOrganizations"), "organizations");

        return scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(NormalizeScope)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddConfiguredValues(ICollection<string> scopes, IConfigurationSection section)
    {
        foreach (var value in section.Get<List<string>>() ?? [])
            scopes.Add(value);
    }

    private static void AddPrefixedValues(ICollection<string> scopes, IConfigurationSection section, string prefix)
    {
        foreach (var value in section.Get<List<string>>() ?? [])
        {
            var trimmed = value.Trim().Trim('/');
            scopes.Add(trimmed.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{prefix}/{trimmed}");
        }
    }

    private static string NormalizeScope(string scope)
    {
        var trimmed = scope.Trim().Trim('/');
        if (trimmed.StartsWith("//cloudresourcemanager.googleapis.com/", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["//cloudresourcemanager.googleapis.com/".Length..];

        return trimmed;
    }
}
