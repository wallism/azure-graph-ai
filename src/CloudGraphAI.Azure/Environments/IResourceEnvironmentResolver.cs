using CloudGraphAI.Azure.Models;
using CloudGraphAI.Azure.Configuration;
using Microsoft.Extensions.Configuration;

namespace CloudGraphAI.Azure.Environments;

public interface IResourceEnvironmentResolver
{
    string? Resolve(AzureResourceNode resource);
}

public sealed class ConfigurationResourceEnvironmentResolver(IConfiguration configuration) : IResourceEnvironmentResolver
{
    private readonly List<EnvironmentRule> _rules = configuration
        .GetSection("AzureGraph:EnvironmentRules")
        .Get<List<EnvironmentRule>>() ?? [];

    public string? Resolve(AzureResourceNode resource)
    {
        foreach (var rule in _rules.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            if (Matches(rule, resource))
                return rule.Name;
        }

        return null;
    }

    private static bool Matches(EnvironmentRule rule, AzureResourceNode resource)
    {
        var hasCriterion = false;

        if (!string.IsNullOrWhiteSpace(rule.TagKey))
        {
            hasCriterion = true;
            var tagValue = resource.GetTagValue(rule.TagKey);
            if (string.IsNullOrWhiteSpace(rule.TagValue))
            {
                if (string.IsNullOrWhiteSpace(tagValue))
                    return false;
            }
            else if (!string.Equals(tagValue, rule.TagValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!ContainsIfConfigured(rule.NameContains, resource.Name, ref hasCriterion))
            return false;

        if (!ContainsIfConfigured(rule.ResourceGroupContains, resource.ResourceGroupName, ref hasCriterion))
            return false;

        if (!ContainsIfConfigured(rule.IdContains, resource.Id, ref hasCriterion))
            return false;

        return hasCriterion;
    }

    private static bool ContainsIfConfigured(string? needle, string? haystack, ref bool hasCriterion)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return true;

        hasCriterion = true;
        return haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;
    }
}
