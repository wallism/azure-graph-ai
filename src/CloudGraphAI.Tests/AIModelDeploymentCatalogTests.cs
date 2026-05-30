using CloudGraphAI.AI.Configuration;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class AIModelDeploymentCatalogTests
{
    [Test]
    public void FromOptions_WithOnlyAzureFoundryEnabled_UsesConfiguredDeployment()
    {
        var options = new AIModelsOptions
        {
            AzureFoundry = new AzureFoundryOptions
            {
                Enabled = true,
                Endpoint = "https://example.services.ai.azure.com/",
                ApiKey = "secret",
                Deployments =
                [
                    new AzureFoundryDeploymentOptions
                    {
                        ServiceId = "azure-chat",
                        DeploymentName = "gpt-chat",
                        Type = "Chat"
                    }
                ]
            }
        };

        var catalog = AIModelDeploymentCatalog.FromOptions(options);

        Assert.That(catalog.DefaultChatServiceId, Is.EqualTo("azure-chat"));
        Assert.That(catalog.Deployments, Has.Count.EqualTo(1));
        Assert.That(catalog.Deployments[0].Provider, Is.EqualTo(AIModelProvider.AzureFoundry));
    }

    [Test]
    public void FromOptions_WithConfiguredDefault_UsesMatchingChatDeployment()
    {
        var options = new AIModelsOptions
        {
            DefaultChatServiceId = "bedrock-chat",
            GoogleVertexAI = new GoogleVertexAIOptions
            {
                Enabled = true,
                ProjectId = "project",
                Location = "us-central1",
                Deployments =
                [
                    new GoogleVertexAIDeploymentOptions
                    {
                        ServiceId = "vertex-chat",
                        ModelId = "gemini-2.5-pro",
                        Type = "Chat"
                    }
                ]
            },
            AwsBedrock = new AwsBedrockOptions
            {
                Enabled = true,
                Region = "us-east-1",
                Deployments =
                [
                    new AwsBedrockDeploymentOptions
                    {
                        ServiceId = "bedrock-chat",
                        ModelId = "anthropic.claude-sonnet-4-20250514-v1:0",
                        Type = "Chat"
                    }
                ]
            }
        };

        var catalog = AIModelDeploymentCatalog.FromOptions(options);

        Assert.That(catalog.DefaultChatServiceId, Is.EqualTo("bedrock-chat"));
    }

    [Test]
    public void FromOptions_WithDuplicateServiceIdsAcrossProviders_Throws()
    {
        var options = new AIModelsOptions
        {
            GoogleVertexAI = new GoogleVertexAIOptions
            {
                Enabled = true,
                ProjectId = "project",
                Location = "us-central1",
                Deployments =
                [
                    new GoogleVertexAIDeploymentOptions
                    {
                        ServiceId = "shared-chat",
                        ModelId = "gemini-2.5-pro",
                        Type = "Chat"
                    }
                ]
            },
            AwsBedrock = new AwsBedrockOptions
            {
                Enabled = true,
                Region = "us-east-1",
                Deployments =
                [
                    new AwsBedrockDeploymentOptions
                    {
                        ServiceId = "shared-chat",
                        ModelId = "anthropic.claude-sonnet-4-20250514-v1:0",
                        Type = "Chat"
                    }
                ]
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AIModelDeploymentCatalog.FromOptions(options));

        Assert.That(ex!.Message, Does.Contain("Duplicate AI model ServiceId"));
    }

    [Test]
    public void FromOptions_WithNoEnabledChatDeployment_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AIModelDeploymentCatalog.FromOptions(new AIModelsOptions()));

        Assert.That(ex!.Message, Does.Contain("at least one enabled chat deployment"));
    }
}
