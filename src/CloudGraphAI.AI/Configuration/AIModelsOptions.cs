namespace CloudGraphAI.AI.Configuration;

public sealed class AIModelsOptions
{
    public const string SectionName = "AIModels";

    public string? DefaultChatServiceId { get; set; }
    public AzureFoundryOptions? AzureFoundry { get; set; }
    public GoogleVertexAIOptions? GoogleVertexAI { get; set; }
    public AwsBedrockOptions? AwsBedrock { get; set; }
}

public sealed class AzureFoundryOptions
{
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Key { get; set; }
    public Dictionary<string, string> EndpointKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AzureFoundryDeploymentOptions> Deployments { get; set; } = [];

    internal string? EffectiveApiKey => FirstUsableSecret(ApiKey, Key);

    internal static string? FirstUsableSecret(params string?[] values)
        => values.FirstOrDefault(IsUsableSecret);

    internal static bool IsUsableSecret(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           !string.Equals(value, "store-in-secrets-locally", StringComparison.OrdinalIgnoreCase);
}

public sealed class AzureFoundryDeploymentOptions
{
    public string? ServiceId { get; set; }
    public string? DeploymentName { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Key { get; set; }
    public string[]? Capabilities { get; set; }

    internal string? EffectiveDeploymentName => string.IsNullOrWhiteSpace(DeploymentName) ? Name : DeploymentName;
    internal string? EffectiveApiKey => AzureFoundryOptions.FirstUsableSecret(ApiKey, Key);
}

public sealed class GoogleVertexAIOptions
{
    public bool Enabled { get; set; }
    public string? ProjectId { get; set; }
    public string? Location { get; set; }
    public string? CredentialsPath { get; set; }
    public List<GoogleVertexAIDeploymentOptions> Deployments { get; set; } = [];
}

public sealed class GoogleVertexAIDeploymentOptions
{
    public string? ServiceId { get; set; }
    public string? ModelId { get; set; }
    public string? Type { get; set; }
    public string[]? Capabilities { get; set; }
}

public sealed class AwsBedrockOptions
{
    public bool Enabled { get; set; }
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public List<AwsBedrockDeploymentOptions> Deployments { get; set; } = [];
}

public sealed class AwsBedrockDeploymentOptions
{
    public string? ServiceId { get; set; }
    public string? ModelId { get; set; }
    public string? Type { get; set; }
    public string[]? Capabilities { get; set; }
}

public enum AIModelProvider
{
    AzureFoundry,
    GoogleVertexAI,
    AwsBedrock
}

public enum AIModelDeploymentType
{
    Chat,
    Embedding
}

public sealed record AIModelDeploymentDescriptor(
    AIModelProvider Provider,
    string ServiceId,
    string ModelId,
    AIModelDeploymentType Type,
    IReadOnlyList<string> Capabilities);
