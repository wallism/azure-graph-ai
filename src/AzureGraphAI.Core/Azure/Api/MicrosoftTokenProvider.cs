using Azure.Core;
using Azure.Identity;
using AzureGraphAI.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace AzureGraphAI.Core.Azure.Api;

public interface IMicrosoftTokenProvider
{
    Task<TokenResponse> GetManagementTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class MicrosoftTokenProvider(
    IConfiguration configuration,
    IMicrosoftLoginApi loginApi)
    : IMicrosoftTokenProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private TokenResponse? _cachedManagementToken;

    public async Task<TokenResponse> GetManagementTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedManagementToken is { } cached && !cached.IsTokenExpired())
            return cached;

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedManagementToken is { } refreshed && !refreshed.IsTokenExpired())
                return refreshed;

            _cachedManagementToken = await LoadManagementTokenAsync(cancellationToken).ConfigureAwait(false);
            return _cachedManagementToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<TokenResponse> LoadManagementTokenAsync(CancellationToken cancellationToken)
    {
        var mode = configuration["AzureGraph:Authentication:Mode"];
        if (string.IsNullOrWhiteSpace(mode))
            mode = HasClientSecretConfiguration()
                ? AzureGraphAuthenticationModes.ClientSecret
                : AzureGraphAuthenticationModes.AzureCli;

        if (mode.Equals(AzureGraphAuthenticationModes.ClientSecret, StringComparison.OrdinalIgnoreCase))
            return await LoadClientSecretTokenAsync(cancellationToken).ConfigureAwait(false);

        if (mode.Equals(AzureGraphAuthenticationModes.AzureCli, StringComparison.OrdinalIgnoreCase))
            return await LoadAzureCliTokenAsync(cancellationToken).ConfigureAwait(false);

        throw new InvalidOperationException($"Unsupported AzureGraph:Authentication:Mode '{mode}'. Use AzureCli or ClientSecret.");
    }

    private async Task<TokenResponse> LoadAzureCliTokenAsync(CancellationToken cancellationToken)
    {
        var tenantId = configuration["Azure:TenantId"];
        var options = new AzureCliCredentialOptions();
        if (!string.IsNullOrWhiteSpace(tenantId))
            options.TenantId = tenantId;

        var credential = new AzureCliCredential(options);
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]),
            cancellationToken).ConfigureAwait(false);

        return new TokenResponse
        {
            AccessToken = token.Token,
            TokenType = "Bearer",
            ExpiresOn = token.ExpiresOn.ToUnixTimeSeconds(),
            LoadedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<TokenResponse> LoadClientSecretTokenAsync(CancellationToken cancellationToken)
    {
        var tenantId = configuration["Azure:TenantId"];
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new InvalidOperationException("Azure:TenantId is required when AzureGraph:Authentication:Mode is ClientSecret.");

        var tokenResponse = await loginApi.GetManagementTokenAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!tokenResponse.WasSuccessful || tokenResponse.Value is null)
            throw new InvalidOperationException($"Failed to get Azure management token: {tokenResponse.Exception?.Message ?? tokenResponse.RawText}");

        tokenResponse.Value.LoadedAt = DateTimeOffset.UtcNow;
        return tokenResponse.Value;
    }

    private bool HasClientSecretConfiguration()
        => !string.IsNullOrWhiteSpace(configuration["APIS:Auth:Microsoft:ClientId"])
           && !string.IsNullOrWhiteSpace(configuration["APIS:Auth:Microsoft:ClientSecret"]);
}
