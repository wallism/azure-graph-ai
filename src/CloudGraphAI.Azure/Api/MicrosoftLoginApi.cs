using Agile.API.Clients;
using Agile.API.Clients.CallHandling;
using Agile.API.Clients.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Api;

public interface IMicrosoftLoginApi
{
    Task<CallResult<TokenResponse>> GetManagementTokenAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class MicrosoftLoginApi : ApiBase, IMicrosoftLoginApi
{
    private readonly ILogger<MicrosoftLoginApi> _logger;
    private readonly ApiMethod<TokenResponse> _postToken;

    public MicrosoftLoginApi(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<MicrosoftLoginApi> logger)
        : base(configuration, httpClientFactory)
    {
        _logger = logger;
        _postToken = PrivatePost<TokenResponse>(MethodPriority.Normal, MediaTypes.FormUrlEncoded);
    }

    protected override string BaseUrl => "https://login.microsoftonline.com";

    public override string ApiId => "MicrosoftLogin";

    public Task<CallResult<TokenResponse>> GetManagementTokenAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var path = $"{tenantId}/oauth2/token";
        var formData = new Dictionary<string, string?>
        {
            ["grant_type"] = "client_credentials",
            ["resource"] = "https://management.azure.com/",
            ["client_id"] = Configuration["APIS:Auth:Microsoft:ClientId"],
            ["client_secret"] = Configuration["APIS:Auth:Microsoft:ClientSecret"]
        }.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

        return _postToken.Call(path, formData, cancellationToken: cancellationToken);
    }

    protected override Task SetPrivateRequestProperties(HttpRequestMessage request, string method, object? rawPayload = null, string propsWithNonce = "")
        => Task.CompletedTask;

    protected override void NotifyError<T>(CallResult<T> result)
        => _logger.LogError("{ApiId} {StatusCode} {Uri} {Error} {RawText}", ApiId, result.StatusCode, result.AbsoluteUri, result.Exception?.Message, result.RawText);
}

public sealed class TokenResponse
{
    [JsonProperty("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonProperty("expires_on")]
    public long? ExpiresOn { get; set; }

    [JsonProperty("expires_in")]
    public long? ExpiresIn { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTimeOffset LoadedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsTokenExpired()
    {
        var expires = ExpiresOn.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(ExpiresOn.Value)
            : LoadedAt.AddSeconds(ExpiresIn ?? 3000);

        return DateTimeOffset.UtcNow >= expires.AddMinutes(-5);
    }
}
