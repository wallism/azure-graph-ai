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

            var tenantId = configuration["Azure:TenantId"];
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Azure:TenantId is required.");

            var tokenResponse = await loginApi.GetManagementTokenAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (!tokenResponse.WasSuccessful || tokenResponse.Value is null)
                throw new InvalidOperationException($"Failed to get Azure management token: {tokenResponse.Exception?.Message ?? tokenResponse.RawText}");

            tokenResponse.Value.LoadedAt = DateTimeOffset.UtcNow;
            _cachedManagementToken = tokenResponse.Value;
            return _cachedManagementToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
