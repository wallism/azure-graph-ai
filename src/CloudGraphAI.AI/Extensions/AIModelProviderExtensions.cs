using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using CloudGraphAI.AI.Configuration;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace CloudGraphAI.AI.Extensions;

public static class AIModelProviderExtensions
{
    private const string GoogleCloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    public static IKernelBuilder AddConfiguredAIModelProviders(
        this IKernelBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(AIModelsOptions.SectionName)
            .Get<AIModelsOptions>()
            ?? throw new InvalidOperationException("AIModels configuration section is missing.");

        var catalog = AIModelDeploymentCatalog.FromOptions(options);

        RegisterAzureFoundryChatDeployments(builder, options.AzureFoundry);
        RegisterGoogleVertexAIChatDeployments(builder, options.GoogleVertexAI);
        RegisterAwsBedrockChatDeployments(builder, options.AwsBedrock);

        builder.Services.AddSingleton<IAIModelDeploymentCatalog>(catalog);
        builder.Services.AddSingleton<IChatCompletionService>(sp =>
            sp.GetRequiredKeyedService<IChatCompletionService>(catalog.DefaultChatServiceId));

        return builder;
    }

    private static void RegisterAzureFoundryChatDeployments(
        IKernelBuilder builder,
        AzureFoundryOptions? options)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        var defaultEndpoint = RequireValue(options.Endpoint, "AIModels:AzureFoundry:Endpoint");
        var defaultApiKey = RequireValue(options.EffectiveApiKey, "AIModels:AzureFoundry:ApiKey");

        foreach (var deployment in options.Deployments.Where(IsChatDeployment))
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:AzureFoundry:Deployments:ServiceId");
            var deploymentName = RequireValue(
                deployment.EffectiveDeploymentName,
                "AIModels:AzureFoundry:Deployments:DeploymentName");
            var endpoint = string.IsNullOrWhiteSpace(deployment.Endpoint)
                ? defaultEndpoint
                : deployment.Endpoint.Trim();
            var apiKey = ResolveAzureFoundryApiKey(options, deployment, endpoint) ?? defaultApiKey;

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey,
                serviceId: serviceId);
        }
    }

    private static void RegisterGoogleVertexAIChatDeployments(
        IKernelBuilder builder,
        GoogleVertexAIOptions? options)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        var projectId = RequireValue(options.ProjectId, "AIModels:GoogleVertexAI:ProjectId");
        var location = RequireValue(options.Location, "AIModels:GoogleVertexAI:Location");
        var bearerTokenProvider = CreateGoogleAccessTokenProvider(options.CredentialsPath);

        foreach (var deployment in options.Deployments.Where(IsChatDeployment))
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:GoogleVertexAI:Deployments:ServiceId");
            var modelId = RequireValue(deployment.ModelId, "AIModels:GoogleVertexAI:Deployments:ModelId");

            builder.AddVertexAIGeminiChatCompletion(
                modelId: modelId,
                bearerTokenProvider: bearerTokenProvider,
                location: location,
                projectId: projectId,
                apiVersion: VertexAIVersion.V1,
                serviceId: serviceId);
        }
    }

    private static void RegisterAwsBedrockChatDeployments(
        IKernelBuilder builder,
        AwsBedrockOptions? options)
    {
        if (options?.Enabled != true)
        {
            return;
        }

        var region = RegionEndpoint.GetBySystemName(RequireValue(options.Region, "AIModels:AwsBedrock:Region"));
        var bedrockRuntime = CreateBedrockRuntimeClient(options, region);
        builder.Services.AddSingleton<IAmazonBedrockRuntime>(bedrockRuntime);

        foreach (var deployment in options.Deployments.Where(IsChatDeployment))
        {
            var serviceId = RequireValue(deployment.ServiceId, "AIModels:AwsBedrock:Deployments:ServiceId");
            var modelId = RequireValue(deployment.ModelId, "AIModels:AwsBedrock:Deployments:ModelId");

            builder.AddBedrockChatCompletionService(
                modelId,
                bedrockRuntime,
                serviceId);
        }
    }

    private static AmazonBedrockRuntimeClient CreateBedrockRuntimeClient(
        AwsBedrockOptions options,
        RegionEndpoint region)
    {
        if (!string.IsNullOrWhiteSpace(options.AccessKeyId) ||
            !string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            var accessKeyId = RequireValue(options.AccessKeyId, "AIModels:AwsBedrock:AccessKeyId");
            var secretAccessKey = RequireValue(options.SecretAccessKey, "AIModels:AwsBedrock:SecretAccessKey");

            return new AmazonBedrockRuntimeClient(
                new BasicAWSCredentials(accessKeyId, secretAccessKey),
                region);
        }

        return new AmazonBedrockRuntimeClient(region);
    }

    private static string? ResolveAzureFoundryApiKey(
        AzureFoundryOptions options,
        AzureFoundryDeploymentOptions deployment,
        string endpoint)
    {
        var deploymentKey = deployment.EffectiveApiKey;
        if (!string.IsNullOrWhiteSpace(deploymentKey))
        {
            return deploymentKey;
        }

        var endpointName = ExtractAzureEndpointName(endpoint).Replace("-", "_");
        return options.EndpointKeys.TryGetValue(endpointName, out var endpointKey) &&
               AzureFoundryOptions.IsUsableSecret(endpointKey)
            ? endpointKey
            : null;
    }

    internal static string ExtractAzureEndpointName(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid Azure Foundry endpoint URL: '{endpoint}'.");
        }

        var host = uri.Host;
        string[] azureSuffixes =
        [
            ".cognitiveservices.azure.com",
            ".openai.azure.com",
            ".services.ai.azure.com"
        ];

        foreach (var suffix in azureSuffixes)
        {
            var suffixIndex = host.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
            if (suffixIndex > 0)
            {
                return host[..suffixIndex];
            }
        }

        return host;
    }

    private static Func<ValueTask<string>> CreateGoogleAccessTokenProvider(string? credentialsPath)
    {
        var credentialTask = new Lazy<Task<GoogleCredential>>(() => LoadGoogleCredentialAsync(credentialsPath));

        return async () =>
        {
            var credential = await credentialTask.Value.ConfigureAwait(false);
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync().ConfigureAwait(false);
        };
    }

    private static async Task<GoogleCredential> LoadGoogleCredentialAsync(string? credentialsPath)
    {
        var credential = string.IsNullOrWhiteSpace(credentialsPath)
            ? await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false)
            : await CredentialFactory
                .FromFileAsync(credentialsPath, credentialType: null!, CancellationToken.None)
                .ConfigureAwait(false);

        return credential.IsCreateScopedRequired
            ? credential.CreateScoped(GoogleCloudPlatformScope)
            : credential;
    }

    private static bool IsChatDeployment(AzureFoundryDeploymentOptions deployment)
        => IsChatDeployment(deployment.Type);

    private static bool IsChatDeployment(GoogleVertexAIDeploymentOptions deployment)
        => IsChatDeployment(deployment.Type);

    private static bool IsChatDeployment(AwsBedrockDeploymentOptions deployment)
        => IsChatDeployment(deployment.Type);

    private static bool IsChatDeployment(string? type)
        => string.IsNullOrWhiteSpace(type) ||
           string.Equals(type, nameof(AIModelDeploymentType.Chat), StringComparison.OrdinalIgnoreCase);

    private static string RequireValue(string? value, string settingPath)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"{settingPath} is required.");
    }
}
