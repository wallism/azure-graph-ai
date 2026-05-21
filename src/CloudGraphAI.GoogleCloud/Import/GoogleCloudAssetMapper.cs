using CloudGraphAI.GoogleCloud.Configuration;
using CloudGraphAI.GoogleCloud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudGraphAI.GoogleCloud.Import;

internal static class GoogleCloudAssetMapper
{
    public static T ApplyCommon<T>(T node, GoogleCloudAsset asset)
        where T : GoogleCloudGraphNode
    {
        node.Id = asset.Name ?? node.Id;
        node.Type = asset.AssetType;
        node.Location = asset.Resource?.Location ?? GoogleCloudResourceNames.GetLocation(asset.Name);
        node.ResourceUrl = asset.Resource?.ResourceUrl;
        node.Parent = asset.Resource?.Parent;
        node.UpdateTime = asset.UpdateTime;
        node.Labels = ToStringDictionary(asset.Resource?.Data?["labels"]);
        return node;
    }

    public static GoogleOrganization ToOrganization(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleOrganization { Id = asset.Name ?? string.Empty }, asset);
        node.OrganizationDisplayName = GetString(data, "displayName");
        node.Name = node.OrganizationDisplayName ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        return node;
    }

    public static GoogleFolder ToFolder(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleFolder { Id = asset.Name ?? string.Empty }, asset);
        node.FolderDisplayName = GetString(data, "displayName");
        node.Name = node.FolderDisplayName ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        return node;
    }

    public static GoogleProject ToProject(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleProject { Id = asset.Name ?? string.Empty }, asset);
        node.ProjectId = GetString(data, "projectId") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.ProjectNumber = GetString(data, "name")?.Replace("projects/", "", StringComparison.OrdinalIgnoreCase);
        node.LifecycleState = GetString(data, "state", "lifecycleState");
        node.Name = GetString(data, "displayName") ?? node.ProjectId;
        return node;
    }

    public static GoogleCloudRunService ToCloudRunService(
        GoogleCloudAsset asset,
        IReadOnlyCollection<string> vertexEndpointSettingNames)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleCloudRunService { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GoogleCloudResourceNames.GetLastSegment(GetString(data, "name") ?? asset.Name);
        node.Uri = GetString(data, "uri");
        node.Ingress = GetString(data, "ingress");
        node.LaunchStage = GetString(data, "launchStage");
        node.ServiceAccount = GetString(data, "template.serviceAccount");

        foreach (var image in GetContainerImages(data))
            AddDistinct(node.ContainerImageReferences, image);

        foreach (var secret in GetSecretReferences(data))
            AddDistinct(node.SecretReferenceCandidates, secret);

        foreach (var endpoint in GetConfiguredEnvValues(data, vertexEndpointSettingNames))
            AddDistinct(node.VertexAiEndpointCandidates, endpoint);

        return node;
    }

    public static GoogleArtifactRepository ToArtifactRepository(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleArtifactRepository { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GoogleCloudResourceNames.GetLastSegment(GetString(data, "name") ?? asset.Name);
        node.Format = GetString(data, "format");
        node.Mode = GetString(data, "mode");
        node.Description = GetString(data, "description");
        return node;
    }

    public static GoogleCloudSqlInstance ToCloudSqlInstance(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleCloudSqlInstance { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GetString(data, "name") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.DatabaseVersion = GetString(data, "databaseVersion");
        node.State = GetString(data, "state");
        node.ConnectionName = GetString(data, "connectionName");
        node.BackendType = GetString(data, "backendType");
        node.PrivateNetworkCandidate = GetString(data, "settings.ipConfiguration.privateNetwork");
        return node;
    }

    public static GoogleStorageBucket ToStorageBucket(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleStorageBucket { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GetString(data, "name") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.StorageClass = GetString(data, "storageClass");
        node.LocationType = GetString(data, "locationType");
        node.PublicAccessPrevention = GetString(data, "iamConfiguration.publicAccessPrevention");
        node.UniformBucketLevelAccess = GetBool(data, "iamConfiguration.uniformBucketLevelAccess.enabled");
        return node;
    }

    public static GoogleSecret ToSecret(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleSecret { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GoogleCloudResourceNames.GetLastSegment(GetString(data, "name") ?? asset.Name);
        node.Replication = data?["replication"] is null ? null : data["replication"]!.ToString(Formatting.None);
        node.CreateTime = GetString(data, "createTime");
        return node;
    }

    public static GoogleNetwork ToNetwork(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleNetwork { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GetString(data, "name") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.AutoCreateSubnetworks = GetBool(data, "autoCreateSubnetworks");
        node.RoutingMode = GetString(data, "routingConfig.routingMode");
        return node;
    }

    public static GoogleSubnetwork ToSubnetwork(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleSubnetwork { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GetString(data, "name") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.IpCidrRange = GetString(data, "ipCidrRange");
        node.GatewayAddress = GetString(data, "gatewayAddress");
        node.NetworkCandidate = GetString(data, "network");
        return node;
    }

    public static GoogleVertexAiEndpoint ToVertexAiEndpoint(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleVertexAiEndpoint { Id = asset.Name ?? string.Empty }, asset);
        node.EndpointDisplayName = GetString(data, "displayName");
        node.Name = node.EndpointDisplayName ?? GoogleCloudResourceNames.GetLastSegment(GetString(data, "name") ?? asset.Name);
        node.CreateTime = GetString(data, "createTime");
        node.BoundaryApiEndpoint = BuildVertexPredictionEndpoint(GetString(data, "name") ?? asset.Name, node.Location);
        return node;
    }

    public static GoogleMemorystoreRedisInstance ToRedisInstance(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleMemorystoreRedisInstance { Id = asset.Name ?? string.Empty }, asset);
        node.Name = GetString(data, "name") ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        node.State = GetString(data, "state");
        node.Host = GetString(data, "host");
        node.Port = GetLong(data, "port");
        node.RedisVersion = GetString(data, "redisVersion");
        node.AuthorizedNetworkCandidate = GetString(data, "authorizedNetwork");
        return node;
    }

    public static GoogleServiceAccount ToServiceAccount(GoogleCloudAsset asset)
    {
        var data = asset.Resource?.Data;
        var node = ApplyCommon(new GoogleServiceAccount { Id = asset.Name ?? string.Empty }, asset);
        node.Email = GetString(data, "email");
        node.AccountDisplayName = GetString(data, "displayName");
        node.Disabled = GetBool(data, "disabled");
        node.Name = node.Email ?? node.AccountDisplayName ?? GoogleCloudResourceNames.GetLastSegment(asset.Name);
        return node;
    }

    public static string? GetString(JObject? data, params string[] paths)
    {
        if (data is null)
            return null;

        foreach (var path in paths)
        {
            var token = data.SelectToken(path);
            if (token is null || token.Type == JTokenType.Null)
                continue;

            var value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static bool? GetBool(JObject? data, string path)
        => data?.SelectToken(path)?.Type switch
        {
            JTokenType.Boolean => data.SelectToken(path)!.Value<bool>(),
            JTokenType.String when bool.TryParse(data.SelectToken(path)!.Value<string>(), out var value) => value,
            _ => null
        };

    private static long? GetLong(JObject? data, string path)
    {
        var token = data?.SelectToken(path);
        if (token is null || token.Type == JTokenType.Null)
            return null;

        return token.Type is JTokenType.Integer or JTokenType.Float
            ? token.Value<long>()
            : long.TryParse(token.Value<string>(), out var value) ? value : null;
    }

    private static Dictionary<string, string?>? ToStringDictionary(JToken? token)
    {
        if (token is not JObject obj)
            return null;

        return obj.Properties()
            .ToDictionary(
                property => property.Name,
                property => property.Value.Type == JTokenType.Null ? null : property.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetContainerImages(JObject? data)
        => data?.SelectTokens("template.containers[*].image")
            .Select(token => token.Value<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase) ?? [];

    private static IEnumerable<string> GetSecretReferences(JObject? data)
    {
        if (data is null)
            yield break;

        foreach (var token in data.SelectTokens("template.containers[*].env[*].valueSource.secretKeyRef.secret"))
        {
            var value = token.Value<string>();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }

        foreach (var token in data.SelectTokens("template.volumes[*].secret.secret"))
        {
            var value = token.Value<string>();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }
    }

    private static IEnumerable<string> GetConfiguredEnvValues(JObject? data, IReadOnlyCollection<string> settingNames)
    {
        if (data is null || settingNames.Count == 0)
            yield break;

        var configuredNames = settingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var env in data.SelectTokens("template.containers[*].env[*]").OfType<JObject>())
        {
            var name = env["name"]?.Value<string>();
            var value = env["value"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(name)
                && !string.IsNullOrWhiteSpace(value)
                && configuredNames.Contains(name))
            {
                yield return value;
            }
        }
    }

    private static string? BuildVertexPredictionEndpoint(string? resourceName, string? location)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            return null;

        var relativeName = resourceName.TrimStart('/');
        if (relativeName.StartsWith("aiplatform.googleapis.com/", StringComparison.OrdinalIgnoreCase))
            relativeName = relativeName["aiplatform.googleapis.com/".Length..];

        if (relativeName.StartsWith("/v1/", StringComparison.OrdinalIgnoreCase))
            relativeName = relativeName[4..];

        var host = string.IsNullOrWhiteSpace(location) || location.Equals("global", StringComparison.OrdinalIgnoreCase)
            ? "aiplatform.googleapis.com"
            : $"{location}-aiplatform.googleapis.com";

        return $"https://{host}/v1/{relativeName}:predict";
    }

    private static void AddDistinct(ICollection<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            values.Add(value);
    }
}
