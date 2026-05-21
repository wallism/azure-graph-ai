namespace CloudGraphAI.GoogleCloud.Configuration;

public sealed class GoogleCloudGraphOptions
{
    public List<string> IncludedScopes { get; set; } = [];

    public GoogleCloudAuthenticationOptions Authentication { get; set; } = new();

    public int PageSize { get; set; } = 500;

    public bool IncludeCloudRunDetails { get; set; } = true;

    public List<string> VertexAiEndpointSettingNames { get; set; } =
    [
        "VertexAI:Endpoint",
        "VertexAI__Endpoint",
        "VertexAIEndpoint",
        "VertexEndpoint",
        "GoogleCloud:VertexAI:Endpoint",
        "GoogleCloud__VertexAI__Endpoint"
    ];

    public List<string> EnvironmentRules { get; set; } = [];
}

public sealed class GoogleCloudAuthenticationOptions
{
    public string Mode { get; set; } = GoogleCloudAuthenticationModes.GCloudCli;

    public string GCloudExecutable { get; set; } = "gcloud";

    public string? AccessToken { get; set; }

    public string? ApiKey { get; set; }

    public string? QuotaProject { get; set; }
}

public static class GoogleCloudAuthenticationModes
{
    public const string GCloudCli = "GCloudCli";
    public const string ApplicationDefaultCredentials = "ApplicationDefaultCredentials";
    public const string AccessToken = "AccessToken";
    public const string ApiKey = "ApiKey";
}
