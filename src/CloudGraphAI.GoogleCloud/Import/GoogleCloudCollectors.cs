using CloudGraphAI.GoogleCloud.Api;
using CloudGraphAI.GoogleCloud.Configuration;
using CloudGraphAI.GoogleCloud.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static CloudGraphAI.GoogleCloud.Import.GoogleCloudRelationshipHelpers;

namespace CloudGraphAI.GoogleCloud.Import;

public sealed class GoogleOrganizationCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleOrganizationCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleOrganization>(googleCloudApi, configuration, logger)
{
    public override int Order => 5;

    protected override IReadOnlyList<string> AssetTypes => ["cloudresourcemanager.googleapis.com/Organization"];

    protected override GoogleOrganization MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToOrganization(asset);
}

public sealed class GoogleFolderCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleFolderCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleFolder>(googleCloudApi, configuration, logger)
{
    public override int Order => 10;

    protected override IReadOnlyList<string> AssetTypes => ["cloudresourcemanager.googleapis.com/Folder"];

    protected override GoogleFolder MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToFolder(asset);
}

public sealed class GoogleProjectCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleProjectCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleProject>(googleCloudApi, configuration, logger)
{
    public override int Order => 20;

    protected override IReadOnlyList<string> AssetTypes => ["cloudresourcemanager.googleapis.com/Project"];

    protected override GoogleProject MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToProject(asset);
}

public sealed class GoogleNetworkCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleNetworkCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleNetwork>(googleCloudApi, configuration, logger)
{
    public override int Order => 40;

    protected override IReadOnlyList<string> AssetTypes => ["compute.googleapis.com/Network"];

    protected override GoogleNetwork MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToNetwork(asset);
}

public sealed class GoogleSubnetworkCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleSubnetworkCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleSubnetwork>(googleCloudApi, configuration, logger)
{
    public override int Order => 45;

    protected override IReadOnlyList<string> AssetTypes => ["compute.googleapis.com/Subnetwork"];

    protected override GoogleSubnetwork MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToSubnetwork(asset);

    public override Task BuildRelationshipsAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var subnetwork in context.GetNodes<GoogleSubnetwork>())
            AddNetworkIfFound(subnetwork.Networks, context, subnetwork.NetworkCandidate);

        return Task.CompletedTask;
    }
}

public sealed class GoogleServiceAccountCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleServiceAccountCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleServiceAccount>(googleCloudApi, configuration, logger)
{
    public override int Order => 50;

    protected override IReadOnlyList<string> AssetTypes => ["iam.googleapis.com/ServiceAccount"];

    protected override GoogleServiceAccount MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToServiceAccount(asset);
}

public sealed class GoogleArtifactRepositoryCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleArtifactRepositoryCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleArtifactRepository>(googleCloudApi, configuration, logger)
{
    public override int Order => 50;

    protected override IReadOnlyList<string> AssetTypes => ["artifactregistry.googleapis.com/Repository"];

    protected override GoogleArtifactRepository MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToArtifactRepository(asset);
}

public sealed class GoogleSecretCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleSecretCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleSecret>(googleCloudApi, configuration, logger)
{
    public override int Order => 50;

    protected override IReadOnlyList<string> AssetTypes => ["secretmanager.googleapis.com/Secret"];

    protected override GoogleSecret MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToSecret(asset);
}

public sealed class GoogleStorageBucketCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleStorageBucketCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleStorageBucket>(googleCloudApi, configuration, logger)
{
    public override int Order => 50;

    protected override IReadOnlyList<string> AssetTypes => ["storage.googleapis.com/Bucket"];

    protected override GoogleStorageBucket MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToStorageBucket(asset);
}

public sealed class GoogleCloudSqlInstanceCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleCloudSqlInstanceCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleCloudSqlInstance>(googleCloudApi, configuration, logger)
{
    public override int Order => 60;

    protected override IReadOnlyList<string> AssetTypes => ["sqladmin.googleapis.com/Instance"];

    protected override GoogleCloudSqlInstance MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToCloudSqlInstance(asset);

    public override Task BuildRelationshipsAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var instance in context.GetNodes<GoogleCloudSqlInstance>())
            AddNetworkIfFound(instance.PrivateNetworks, context, instance.PrivateNetworkCandidate);

        return Task.CompletedTask;
    }
}

public sealed class GoogleVertexAiEndpointCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleVertexAiEndpointCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleVertexAiEndpoint>(googleCloudApi, configuration, logger)
{
    public override int Order => 60;

    protected override IReadOnlyList<string> AssetTypes => ["aiplatform.googleapis.com/Endpoint"];

    protected override GoogleVertexAiEndpoint MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToVertexAiEndpoint(asset);
}

public sealed class GoogleMemorystoreRedisCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleMemorystoreRedisCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleMemorystoreRedisInstance>(googleCloudApi, configuration, logger)
{
    public override int Order => 60;

    protected override IReadOnlyList<string> AssetTypes => ["redis.googleapis.com/Instance"];

    protected override GoogleMemorystoreRedisInstance MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToRedisInstance(asset);

    public override Task BuildRelationshipsAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var instance in context.GetNodes<GoogleMemorystoreRedisInstance>())
            AddNetworkIfFound(instance.Networks, context, instance.AuthorizedNetworkCandidate);

        return Task.CompletedTask;
    }
}

public sealed class GoogleCloudRunServiceCollector(
    IGoogleCloudRestApi googleCloudApi,
    IConfiguration configuration,
    ILogger<GoogleCloudRunServiceCollector> logger)
    : GoogleCloudResourceCollectorBase<GoogleCloudRunService>(googleCloudApi, configuration, logger)
{
    private readonly GoogleCloudGraphOptions _options = configuration.GetSection("GoogleCloudGraph").Get<GoogleCloudGraphOptions>() ?? new();

    public override int Order => 70;

    protected override IReadOnlyList<string> AssetTypes => ["run.googleapis.com/Service"];

    protected override GoogleCloudRunService MapAsset(GoogleCloudAsset asset)
        => GoogleCloudAssetMapper.ToCloudRunService(asset, _options.VertexAiEndpointSettingNames);

    public override Task BuildRelationshipsAsync(GoogleCloudImportContext context, CancellationToken cancellationToken = default)
    {
        foreach (var service in context.GetNodes<GoogleCloudRunService>())
        {
            foreach (var image in service.ContainerImageReferences)
                AddArtifactRepositoryIfFound(service.PullsFromRegistries, context, image);

            foreach (var secret in service.SecretReferenceCandidates)
                AddSecretIfFound(service.Secrets, context, secret, service.ProjectId);

            AddServiceAccountIfFound(service.ServiceAccounts, context, service.ServiceAccount);
            AddVertexEndpointMatches(service.VertexAiEndpoints, context.GetNodes<GoogleVertexAiEndpoint>(), service.VertexAiEndpointCandidates);
        }

        return Task.CompletedTask;
    }
}

internal static class GoogleCloudRelationshipHelpers
{
    public static void AddNetworkIfFound(ICollection<string> relationshipIds, GoogleCloudImportContext context, string? candidate)
    {
        var normalized = GoogleCloudResourceNames.FromComputeSelfLink(candidate);
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        var network = context.FindById<GoogleNetwork>(normalized);
        if (network is not null && !relationshipIds.Contains(network.Id, StringComparer.OrdinalIgnoreCase))
            relationshipIds.Add(network.Id);
    }

    public static void AddArtifactRepositoryIfFound(ICollection<string> relationshipIds, GoogleCloudImportContext context, string? image)
    {
        var repositoryId = TryBuildArtifactRepositoryId(image);
        if (string.IsNullOrWhiteSpace(repositoryId))
            return;

        var repository = context.FindById<GoogleArtifactRepository>(repositoryId);
        if (repository is not null && !relationshipIds.Contains(repository.Id, StringComparer.OrdinalIgnoreCase))
            relationshipIds.Add(repository.Id);
    }

    public static void AddSecretIfFound(ICollection<string> relationshipIds, GoogleCloudImportContext context, string? candidate, string? projectId)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        var normalizedCandidates = BuildSecretCandidates(candidate, projectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secret = context.Find<GoogleSecret>(secret =>
            normalizedCandidates.Contains(secret.Id)
            || (!string.IsNullOrWhiteSpace(secret.Name) && normalizedCandidates.Contains(secret.Name)));

        if (secret is not null && !relationshipIds.Contains(secret.Id, StringComparer.OrdinalIgnoreCase))
            relationshipIds.Add(secret.Id);
    }

    public static void AddServiceAccountIfFound(ICollection<string> relationshipIds, GoogleCloudImportContext context, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var account = context.Find<GoogleServiceAccount>(candidate =>
            candidate.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true
            || candidate.Id.EndsWith($"/serviceAccounts/{email}", StringComparison.OrdinalIgnoreCase));

        if (account is not null && !relationshipIds.Contains(account.Id, StringComparer.OrdinalIgnoreCase))
            relationshipIds.Add(account.Id);
    }

    public static void AddVertexEndpointMatches(
        ICollection<string> relationshipIds,
        IEnumerable<GoogleVertexAiEndpoint> endpoints,
        IEnumerable<string> candidates)
    {
        var normalizedCandidates = candidates
            .SelectMany(BuildVertexEndpointKeys)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedCandidates.Count == 0)
            return;

        foreach (var endpoint in endpoints)
        {
            if (BuildVertexEndpointKeys(endpoint.Id)
                    .Concat(BuildVertexEndpointKeys(endpoint.BoundaryApiEndpoint))
                    .Concat(BuildVertexEndpointKeys(endpoint.EndpointDisplayName))
                    .Any(normalizedCandidates.Contains)
                && !relationshipIds.Contains(endpoint.Id, StringComparer.OrdinalIgnoreCase))
            {
                relationshipIds.Add(endpoint.Id);
            }
        }
    }

    private static string? TryBuildArtifactRepositoryId(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
            return null;

        var parts = image.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return null;

        var host = parts[0];
        if (!host.EndsWith("-docker.pkg.dev", StringComparison.OrdinalIgnoreCase))
            return null;

        var location = host[..^"-docker.pkg.dev".Length];
        var project = parts[1];
        var repository = parts[2];
        return $"//artifactregistry.googleapis.com/projects/{project}/locations/{location}/repositories/{repository}";
    }

    private static IEnumerable<string> BuildSecretCandidates(string candidate, string? projectId)
    {
        var trimmed = candidate.Trim().Trim('"', '\'').TrimEnd('/');
        yield return trimmed;
        yield return GoogleCloudResourceNames.GetLastSegment(trimmed);

        if (trimmed.StartsWith("projects/", StringComparison.OrdinalIgnoreCase))
            yield return $"//secretmanager.googleapis.com/{trimmed}";

        if (!string.IsNullOrWhiteSpace(projectId) && !trimmed.Contains('/'))
            yield return $"//secretmanager.googleapis.com/projects/{projectId}/secrets/{trimmed}";
    }

    private static IEnumerable<string> BuildVertexEndpointKeys(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        var trimmed = value.Trim().Trim('"', '\'').TrimEnd('/');
        yield return trimmed;
        yield return GoogleCloudResourceNames.GetLastSegment(trimmed);

        if (trimmed.StartsWith("//aiplatform.googleapis.com/", StringComparison.OrdinalIgnoreCase))
            yield return trimmed["//aiplatform.googleapis.com/".Length..];

        if (trimmed.Contains("/v1/projects/", StringComparison.OrdinalIgnoreCase))
        {
            var v1Index = trimmed.IndexOf("/v1/projects/", StringComparison.OrdinalIgnoreCase);
            yield return trimmed[(v1Index + 4)..].TrimEnd('/').Replace(":predict", "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
