using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class ContainerApp : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerAppProperties? Properties { get; set; }

    [NodeRelationship<ContainerRegistry>(Edges.PullsFromRegistry)]
    public List<string> PullsFromRegistries { get; set; } = [];

    [NodeRelationship<AzureAIFoundryAccount>(Edges.ConnectsToAzureAIFoundry)]
    public List<string> AzureAIFoundryAccounts { get; set; } = [];

    [NodeProperty("runningStatus")]
    [JsonIgnore]
    public string? RunningStatus => Properties?.RunningStatus;

    [NodeProperty("latestRevisionFqdn")]
    [JsonIgnore]
    public string? LatestRevisionFqdn => Properties?.LatestRevisionFqdn;

    public static class Edges
    {
        public const string PullsFromRegistry = "PULLS_FROM_REGISTRY";
        public const string ConnectsToAzureAIFoundry = "CONNECTS_TO_AI_FOUNDRY";
    }
}

public sealed class ContainerAppProperties
{
    [JsonProperty("configuration", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerAppConfiguration? Configuration { get; set; }

    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }

    [JsonProperty("runningStatus", NullValueHandling = NullValueHandling.Ignore)]
    public string? RunningStatus { get; set; }

    [JsonProperty("managedEnvironmentId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ManagedEnvironmentId { get; set; }

    [JsonProperty("latestRevisionFqdn", NullValueHandling = NullValueHandling.Ignore)]
    public string? LatestRevisionFqdn { get; set; }

    [JsonProperty("template", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerAppTemplate? Template { get; set; }
}

public sealed class ContainerAppConfiguration
{
    [JsonProperty("registries", NullValueHandling = NullValueHandling.Ignore)]
    public List<ContainerAppRegistry> Registries { get; set; } = [];

    [JsonProperty("ingress", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerAppIngress? Ingress { get; set; }
}

public sealed class ContainerAppRegistry
{
    [JsonProperty("server", NullValueHandling = NullValueHandling.Ignore)]
    public string? Server { get; set; }
}

public sealed class ContainerAppIngress
{
    [JsonProperty("fqdn", NullValueHandling = NullValueHandling.Ignore)]
    public string? Fqdn { get; set; }

    [JsonProperty("external", NullValueHandling = NullValueHandling.Ignore)]
    public bool? External { get; set; }

    [JsonProperty("targetPort", NullValueHandling = NullValueHandling.Ignore)]
    public long? TargetPort { get; set; }
}

public sealed class ContainerAppTemplate
{
    [JsonProperty("containers", NullValueHandling = NullValueHandling.Ignore)]
    public List<ContainerAppContainer> Containers { get; set; } = [];

    [JsonProperty("scale", NullValueHandling = NullValueHandling.Ignore)]
    public ContainerAppScale? Scale { get; set; }
}

public sealed class ContainerAppContainer
{
    [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
    public string? Image { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("env", NullValueHandling = NullValueHandling.Ignore)]
    public List<ContainerAppEnvironmentVariable> Env { get; set; } = [];
}

public sealed class ContainerAppEnvironmentVariable
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public string? Value { get; set; }

    [JsonProperty("secretRef", NullValueHandling = NullValueHandling.Ignore)]
    public string? SecretRef { get; set; }
}

public sealed class ContainerAppScale
{
    [JsonProperty("minReplicas", NullValueHandling = NullValueHandling.Ignore)]
    public long? MinReplicas { get; set; }

    [JsonProperty("maxReplicas", NullValueHandling = NullValueHandling.Ignore)]
    public long? MaxReplicas { get; set; }
}
