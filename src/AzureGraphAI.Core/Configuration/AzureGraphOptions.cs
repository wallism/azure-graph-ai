namespace AzureGraphAI.Core.Configuration;

public sealed class AzureGraphOptions
{
    public List<string> IncludedSubscriptions { get; set; } = [];

    public bool IncludeWebAppDetails { get; set; } = true;

    public bool IncludeWebJobs { get; set; } = true;

    public List<EnvironmentRule> EnvironmentRules { get; set; } = [];
}

public sealed class EnvironmentRule
{
    public string Name { get; set; } = string.Empty;

    public string? TagKey { get; set; }

    public string? TagValue { get; set; }

    public string? NameContains { get; set; }

    public string? ResourceGroupContains { get; set; }

    public string? IdContains { get; set; }
}
