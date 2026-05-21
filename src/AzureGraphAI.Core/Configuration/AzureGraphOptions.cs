namespace AzureGraphAI.Core.Configuration;

public sealed class AzureGraphOptions
{
    public List<string> IncludedSubscriptions { get; set; } = [];

    public AzureGraphAuthenticationOptions Authentication { get; set; } = new();

    public bool IncludeWebAppDetails { get; set; } = true;

    public bool IncludeWebJobs { get; set; } = true;

    public List<EnvironmentRule> EnvironmentRules { get; set; } = [];
}

public sealed class AzureGraphAuthenticationOptions
{
    public string Mode { get; set; } = AzureGraphAuthenticationModes.AzureCli;
}

public static class AzureGraphAuthenticationModes
{
    public const string AzureCli = "AzureCli";
    public const string ClientSecret = "ClientSecret";
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
