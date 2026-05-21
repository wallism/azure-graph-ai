using System.Net.Http.Headers;
using CloudGraphAI.GoogleCloud.Configuration;
using CloudGraphAI.GoogleCloud.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CloudGraphAI.GoogleCloud.Api;

public interface IGoogleCloudRestApi
{
    Task<GoogleCloudAssetListResult> ListAssetsPageAsync(
        string scope,
        IReadOnlyList<string> assetTypes,
        int pageSize,
        string? pageToken = null,
        CancellationToken cancellationToken = default);
}

public sealed class GoogleCloudRestApi(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IGoogleCloudTokenProvider tokenProvider,
    ILogger<GoogleCloudRestApi> logger)
    : IGoogleCloudRestApi
{
    public const string HttpClientName = nameof(GoogleCloudRestApi);

    private readonly GoogleCloudGraphOptions _options = configuration.GetSection("GoogleCloudGraph").Get<GoogleCloudGraphOptions>() ?? new();

    public async Task<GoogleCloudAssetListResult> ListAssetsPageAsync(
        string scope,
        IReadOnlyList<string> assetTypes,
        int pageSize,
        string? pageToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Google Cloud scope is required.", nameof(scope));

        var credential = await tokenProvider.GetCredentialAsync(cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildListAssetsUri(scope, assetTypes, pageSize, pageToken, credential?.ApiKey));

        if (!string.IsNullOrWhiteSpace(credential?.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue(credential.TokenType ?? "Bearer", credential.AccessToken);

        if (!string.IsNullOrWhiteSpace(_options.Authentication.QuotaProject))
            request.Headers.TryAddWithoutValidation("x-goog-user-project", _options.Authentication.QuotaProject);

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Google Cloud Asset Inventory request failed: {StatusCode} {Uri} {Body}", response.StatusCode, request.RequestUri, raw);
            throw new InvalidOperationException($"Google Cloud Asset Inventory request failed with {(int)response.StatusCode} {response.StatusCode}: {raw}");
        }

        return JsonConvert.DeserializeObject<GoogleCloudAssetListResult>(raw) ?? new GoogleCloudAssetListResult();
    }

    private Uri BuildListAssetsUri(
        string scope,
        IReadOnlyList<string> assetTypes,
        int pageSize,
        string? pageToken,
        string? apiKey)
    {
        var query = new List<string>
        {
            "contentType=RESOURCE",
            $"pageSize={Math.Clamp(pageSize, 1, 1000)}"
        };

        foreach (var assetType in assetTypes.Where(type => !string.IsNullOrWhiteSpace(type)))
            query.Add($"assetTypes={Uri.EscapeDataString(assetType)}");

        if (!string.IsNullOrWhiteSpace(pageToken))
            query.Add($"pageToken={Uri.EscapeDataString(pageToken)}");

        if (!string.IsNullOrWhiteSpace(apiKey))
            query.Add($"key={Uri.EscapeDataString(apiKey)}");

        var trimmedScope = scope.Trim().Trim('/');
        return new Uri($"https://cloudasset.googleapis.com/v1/{trimmedScope}/assets?{string.Join("&", query)}");
    }
}
