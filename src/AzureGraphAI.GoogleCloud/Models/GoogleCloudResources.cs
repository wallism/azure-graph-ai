using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.GoogleCloud.Models;

public sealed class GoogleOrganization : GoogleCloudGraphNode
{
    [NodeProperty("displayName")]
    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? OrganizationDisplayName { get; set; }
}

public sealed class GoogleFolder : GoogleCloudGraphNode
{
    [NodeRelationship<GoogleOrganization>(Edges.InOrganization)]
    public List<string> Organizations { get; set; } = [];

    [NodeRelationship<GoogleFolder>(Edges.InFolder)]
    public List<string> ParentFolders { get; set; } = [];

    [NodeProperty("displayName")]
    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? FolderDisplayName { get; set; }

    public static class Edges
    {
        public const string InOrganization = "IN_ORGANIZATION";
        public const string InFolder = "IN_FOLDER";
    }
}

public sealed class GoogleProject : GoogleCloudGraphNode
{
    [NodeRelationship<GoogleOrganization>(Edges.InOrganization)]
    public List<string> Organizations { get; set; } = [];

    [NodeRelationship<GoogleFolder>(Edges.InFolder)]
    public List<string> Folders { get; set; } = [];

    [NodeProperty("projectId")]
    [JsonProperty("projectId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProjectId { get; set; }

    [NodeProperty("projectNumber")]
    [JsonProperty("projectNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProjectNumber { get; set; }

    [NodeProperty("lifecycleState")]
    [JsonProperty("lifecycleState", NullValueHandling = NullValueHandling.Ignore)]
    public string? LifecycleState { get; set; }

    public static class Edges
    {
        public const string InOrganization = "IN_ORGANIZATION";
        public const string InFolder = "IN_FOLDER";
    }
}

public sealed class GoogleCloudRunService : GoogleCloudResourceNode
{
    [NodeRelationship<GoogleArtifactRepository>(Edges.PullsFromRegistry)]
    public List<string> PullsFromRegistries { get; set; } = [];

    [NodeRelationship<GoogleSecret>(Edges.UsesSecret)]
    public List<string> Secrets { get; set; } = [];

    [NodeRelationship<GoogleServiceAccount>(Edges.RunsAsServiceAccount)]
    public List<string> ServiceAccounts { get; set; } = [];

    [NodeRelationship<GoogleVertexAiEndpoint>(Edges.ConnectsToVertexAi)]
    public List<string> VertexAiEndpoints { get; set; } = [];

    [NodeProperty("uri")]
    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string? Uri { get; set; }

    [NodeProperty("ingress")]
    [JsonProperty("ingress", NullValueHandling = NullValueHandling.Ignore)]
    public string? Ingress { get; set; }

    [NodeProperty("launchStage")]
    [JsonProperty("launchStage", NullValueHandling = NullValueHandling.Ignore)]
    public string? LaunchStage { get; set; }

    [NodeProperty("serviceAccount")]
    [JsonProperty("serviceAccount", NullValueHandling = NullValueHandling.Ignore)]
    public string? ServiceAccount { get; set; }

    [NodeProperty("containerImages")]
    [JsonIgnore]
    public string? ContainerImages => ContainerImageReferences.Count > 0
        ? string.Join(", ", ContainerImageReferences.Order(StringComparer.OrdinalIgnoreCase))
        : null;

    [JsonIgnore]
    public List<string> ContainerImageReferences { get; } = [];

    [JsonIgnore]
    public List<string> SecretReferenceCandidates { get; } = [];

    [JsonIgnore]
    public List<string> VertexAiEndpointCandidates { get; } = [];

    public static class Edges
    {
        public const string PullsFromRegistry = "PULLS_FROM_REGISTRY";
        public const string UsesSecret = "USES_SECRET";
        public const string RunsAsServiceAccount = "RUNS_AS_SERVICE_ACCOUNT";
        public const string ConnectsToVertexAi = "CONNECTS_TO_VERTEX_AI";
    }
}

public sealed class GoogleArtifactRepository : GoogleCloudResourceNode
{
    [NodeProperty("format")]
    [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
    public string? Format { get; set; }

    [NodeProperty("mode")]
    [JsonProperty("mode", NullValueHandling = NullValueHandling.Ignore)]
    public string? Mode { get; set; }

    [NodeProperty("description")]
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }
}

public sealed class GoogleCloudSqlInstance : GoogleCloudResourceNode
{
    [NodeRelationship<GoogleNetwork>(Edges.UsesPrivateNetwork)]
    public List<string> PrivateNetworks { get; set; } = [];

    [NodeProperty("databaseVersion")]
    [JsonProperty("databaseVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? DatabaseVersion { get; set; }

    [NodeProperty("state")]
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }

    [NodeProperty("connectionName")]
    [JsonProperty("connectionName", NullValueHandling = NullValueHandling.Ignore)]
    public string? ConnectionName { get; set; }

    [NodeProperty("backendType")]
    [JsonProperty("backendType", NullValueHandling = NullValueHandling.Ignore)]
    public string? BackendType { get; set; }

    public string? PrivateNetworkCandidate { get; set; }

    public static class Edges
    {
        public const string UsesPrivateNetwork = "USES_PRIVATE_NETWORK";
    }
}

public sealed class GoogleStorageBucket : GoogleCloudResourceNode
{
    [NodeProperty("storageClass")]
    [JsonProperty("storageClass", NullValueHandling = NullValueHandling.Ignore)]
    public string? StorageClass { get; set; }

    [NodeProperty("locationType")]
    [JsonProperty("locationType", NullValueHandling = NullValueHandling.Ignore)]
    public string? LocationType { get; set; }

    [NodeProperty("publicAccessPrevention")]
    [JsonProperty("publicAccessPrevention", NullValueHandling = NullValueHandling.Ignore)]
    public string? PublicAccessPrevention { get; set; }

    [NodeProperty("uniformBucketLevelAccess")]
    [JsonProperty("uniformBucketLevelAccess", NullValueHandling = NullValueHandling.Ignore)]
    public bool? UniformBucketLevelAccess { get; set; }
}

public sealed class GoogleSecret : GoogleCloudResourceNode
{
    [NodeProperty("replication")]
    [JsonProperty("replication", NullValueHandling = NullValueHandling.Ignore)]
    public string? Replication { get; set; }

    [NodeProperty("createTime")]
    [JsonProperty("createTime", NullValueHandling = NullValueHandling.Ignore)]
    public string? CreateTime { get; set; }
}

public sealed class GoogleNetwork : GoogleCloudResourceNode
{
    [NodeProperty("autoCreateSubnetworks")]
    [JsonProperty("autoCreateSubnetworks", NullValueHandling = NullValueHandling.Ignore)]
    public bool? AutoCreateSubnetworks { get; set; }

    [NodeProperty("routingMode")]
    [JsonProperty("routingMode", NullValueHandling = NullValueHandling.Ignore)]
    public string? RoutingMode { get; set; }
}

public sealed class GoogleSubnetwork : GoogleCloudResourceNode
{
    [NodeRelationship<GoogleNetwork>(Edges.InNetwork)]
    public List<string> Networks { get; set; } = [];

    [NodeProperty("ipCidrRange")]
    [JsonProperty("ipCidrRange", NullValueHandling = NullValueHandling.Ignore)]
    public string? IpCidrRange { get; set; }

    [NodeProperty("gatewayAddress")]
    [JsonProperty("gatewayAddress", NullValueHandling = NullValueHandling.Ignore)]
    public string? GatewayAddress { get; set; }

    public string? NetworkCandidate { get; set; }

    public static class Edges
    {
        public const string InNetwork = "IN_NETWORK";
    }
}

public sealed class GoogleVertexAiEndpoint : GoogleCloudResourceNode
{
    [NodeProperty("displayName")]
    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? EndpointDisplayName { get; set; }

    [NodeProperty("boundaryApiEndpoint")]
    [JsonProperty("boundaryApiEndpoint", NullValueHandling = NullValueHandling.Ignore)]
    public string? BoundaryApiEndpoint { get; set; }

    [NodeProperty("createTime")]
    [JsonProperty("createTime", NullValueHandling = NullValueHandling.Ignore)]
    public string? CreateTime { get; set; }
}

public sealed class GoogleMemorystoreRedisInstance : GoogleCloudResourceNode
{
    [NodeRelationship<GoogleNetwork>(Edges.UsesNetwork)]
    public List<string> Networks { get; set; } = [];

    [NodeProperty("state")]
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; set; }

    [NodeProperty("host")]
    [JsonProperty("host", NullValueHandling = NullValueHandling.Ignore)]
    public string? Host { get; set; }

    [NodeProperty("port")]
    [JsonProperty("port", NullValueHandling = NullValueHandling.Ignore)]
    public long? Port { get; set; }

    [NodeProperty("redisVersion")]
    [JsonProperty("redisVersion", NullValueHandling = NullValueHandling.Ignore)]
    public string? RedisVersion { get; set; }

    public string? AuthorizedNetworkCandidate { get; set; }

    public static class Edges
    {
        public const string UsesNetwork = "USES_NETWORK";
    }
}

public sealed class GoogleServiceAccount : GoogleCloudResourceNode
{
    [NodeProperty("email")]
    [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
    public string? Email { get; set; }

    [NodeProperty("displayName")]
    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? AccountDisplayName { get; set; }

    [NodeProperty("disabled")]
    [JsonProperty("disabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Disabled { get; set; }
}
