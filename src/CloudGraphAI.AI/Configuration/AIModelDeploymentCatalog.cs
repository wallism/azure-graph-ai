namespace CloudGraphAI.AI.Configuration;

public interface IAIModelDeploymentCatalog
{
    IReadOnlyList<AIModelDeploymentDescriptor> Deployments { get; }
    string DefaultChatServiceId { get; }
    AIModelDeploymentDescriptor? FindByServiceId(string serviceId);
}

public sealed class AIModelDeploymentCatalog : IAIModelDeploymentCatalog
{
    private readonly IReadOnlyDictionary<string, AIModelDeploymentDescriptor> _deploymentsByServiceId;

    private AIModelDeploymentCatalog(
        IReadOnlyList<AIModelDeploymentDescriptor> deployments,
        string defaultChatServiceId)
    {
        Deployments = deployments;
        DefaultChatServiceId = defaultChatServiceId;
        _deploymentsByServiceId = deployments.ToDictionary(d => d.ServiceId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AIModelDeploymentDescriptor> Deployments { get; }
    public string DefaultChatServiceId { get; }

    public static AIModelDeploymentCatalog FromOptions(AIModelsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var deployments = new List<AIModelDeploymentDescriptor>();
        AddAzureFoundryDeployments(options.AzureFoundry, deployments);
        AddGoogleVertexAIDeployments(options.GoogleVertexAI, deployments);
        AddAwsBedrockDeployments(options.AwsBedrock, deployments);

        ValidateUniqueServiceIds(deployments);

        var chatDeployments = deployments
            .Where(d => d.Type == AIModelDeploymentType.Chat)
            .ToList();

        if (chatDeployments.Count == 0)
        {
            throw new InvalidOperationException(
                "AIModels must configure at least one enabled chat deployment under AzureFoundry, GoogleVertexAI, or AwsBedrock.");
        }

        var defaultChatServiceId = ResolveDefaultChatServiceId(options.DefaultChatServiceId, chatDeployments);
        return new AIModelDeploymentCatalog(deployments, defaultChatServiceId);
    }

    public AIModelDeploymentDescriptor? FindByServiceId(string serviceId)
        => string.IsNullOrWhiteSpace(serviceId)
            ? null
            : _deploymentsByServiceId.GetValueOrDefault(serviceId.Trim());

    private static void AddAzureFoundryDeployments(
        AzureFoundryOptions? options,
        ICollection<AIModelDeploymentDescriptor> deployments)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        RequireValue(options.Endpoint, "AIModels:AzureFoundry:Endpoint");
        RequireValue(options.EffectiveApiKey, "AIModels:AzureFoundry:ApiKey");
        RequireDeployments(options.Deployments.Count, "AIModels:AzureFoundry:Deployments");

        foreach (var deployment in options.Deployments)
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:AzureFoundry:Deployments:ServiceId");
            var deploymentName = RequireValue(
                deployment.EffectiveDeploymentName,
                "AIModels:AzureFoundry:Deployments:DeploymentName");

            deployments.Add(new AIModelDeploymentDescriptor(
                AIModelProvider.AzureFoundry,
                serviceId,
                deploymentName,
                ParseDeploymentType(deployment.Type, $"Azure Foundry deployment '{serviceId}'"),
                deployment.Capabilities ?? []));
        }
    }

    private static void AddGoogleVertexAIDeployments(
        GoogleVertexAIOptions? options,
        ICollection<AIModelDeploymentDescriptor> deployments)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        RequireValue(options.ProjectId, "AIModels:GoogleVertexAI:ProjectId");
        RequireValue(options.Location, "AIModels:GoogleVertexAI:Location");
        RequireDeployments(options.Deployments.Count, "AIModels:GoogleVertexAI:Deployments");

        foreach (var deployment in options.Deployments)
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:GoogleVertexAI:Deployments:ServiceId");
            var modelId = RequireValue(deployment.ModelId, "AIModels:GoogleVertexAI:Deployments:ModelId");

            deployments.Add(new AIModelDeploymentDescriptor(
                AIModelProvider.GoogleVertexAI,
                serviceId,
                modelId,
                ParseDeploymentType(deployment.Type, $"Google Vertex AI deployment '{serviceId}'"),
                deployment.Capabilities ?? []));
        }
    }

    private static void AddAwsBedrockDeployments(
        AwsBedrockOptions? options,
        ICollection<AIModelDeploymentDescriptor> deployments)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        RequireValue(options.Region, "AIModels:AwsBedrock:Region");
        RequireDeployments(options.Deployments.Count, "AIModels:AwsBedrock:Deployments");

        foreach (var deployment in options.Deployments)
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:AwsBedrock:Deployments:ServiceId");
            var modelId = RequireValue(deployment.ModelId, "AIModels:AwsBedrock:Deployments:ModelId");

            deployments.Add(new AIModelDeploymentDescriptor(
                AIModelProvider.AwsBedrock,
                serviceId,
                modelId,
                ParseDeploymentType(deployment.Type, $"AWS Bedrock deployment '{serviceId}'"),
                deployment.Capabilities ?? []));
        }
    }

    private static string ResolveDefaultChatServiceId(
        string? configuredDefault,
        IReadOnlyList<AIModelDeploymentDescriptor> chatDeployments)
    {
        if (string.IsNullOrWhiteSpace(configuredDefault))
        {
            return chatDeployments[0].ServiceId;
        }

        var serviceId = configuredDefault.Trim();
        if (chatDeployments.Any(d => string.Equals(d.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)))
        {
            return serviceId;
        }

        throw new InvalidOperationException(
            $"AIModels:DefaultChatServiceId '{serviceId}' does not match any configured chat deployment. " +
            $"Available chat ServiceIds: {string.Join(", ", chatDeployments.Select(d => d.ServiceId))}");
    }

    private static void ValidateUniqueServiceIds(IReadOnlyList<AIModelDeploymentDescriptor> deployments)
    {
        var serviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var deployment in deployments)
        {
            if (!serviceIds.Add(deployment.ServiceId))
            {
                throw new InvalidOperationException(
                    $"Duplicate AI model ServiceId '{deployment.ServiceId}'. ServiceIds must be unique across providers.");
            }
        }
    }

    private static AIModelDeploymentType ParseDeploymentType(string? value, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AIModelDeploymentType.Chat;
        }

        if (Enum.TryParse<AIModelDeploymentType>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"{context} has invalid Type '{value}'. Valid values are: {string.Join(", ", Enum.GetNames<AIModelDeploymentType>())}.");
    }

    private static string RequireValue(string? value, string settingPath)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"{settingPath} is required.");
    }

    private static void RequireDeployments(int count, string settingPath)
    {
        if (count == 0)
        {
            throw new InvalidOperationException($"{settingPath} must include at least one deployment when the provider is enabled.");
        }
    }
}
