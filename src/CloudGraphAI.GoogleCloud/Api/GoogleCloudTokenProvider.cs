using System.Diagnostics;
using CloudGraphAI.GoogleCloud.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudGraphAI.GoogleCloud.Api;

public interface IGoogleCloudTokenProvider
{
    Task<GoogleCloudAccessCredential?> GetCredentialAsync(CancellationToken cancellationToken = default);
}

public sealed record GoogleCloudAccessCredential(
    string? TokenType,
    string? AccessToken,
    string? ApiKey);

public sealed class GoogleCloudTokenProvider(
    IConfiguration configuration,
    ILogger<GoogleCloudTokenProvider> logger)
    : IGoogleCloudTokenProvider
{
    private readonly GoogleCloudGraphOptions _options = configuration.GetSection("GoogleCloudGraph").Get<GoogleCloudGraphOptions>() ?? new();

    public async Task<GoogleCloudAccessCredential?> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        var mode = _options.Authentication.Mode;
        if (mode.Equals(GoogleCloudAuthenticationModes.ApiKey, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_options.Authentication.ApiKey))
                throw new InvalidOperationException("GoogleCloudGraph:Authentication:ApiKey is required when authentication mode is ApiKey.");

            return new GoogleCloudAccessCredential(null, null, _options.Authentication.ApiKey);
        }

        if (mode.Equals(GoogleCloudAuthenticationModes.AccessToken, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_options.Authentication.AccessToken))
                throw new InvalidOperationException("GoogleCloudGraph:Authentication:AccessToken is required when authentication mode is AccessToken.");

            return new GoogleCloudAccessCredential("Bearer", _options.Authentication.AccessToken, null);
        }

        var token = mode.Equals(GoogleCloudAuthenticationModes.ApplicationDefaultCredentials, StringComparison.OrdinalIgnoreCase)
            ? await RunGCloudAsync("auth application-default print-access-token", cancellationToken).ConfigureAwait(false)
            : await RunGCloudAsync("auth print-access-token", cancellationToken).ConfigureAwait(false);

        return new GoogleCloudAccessCredential("Bearer", token, null);
    }

    private async Task<string> RunGCloudAsync(string arguments, CancellationToken cancellationToken)
    {
        var executable = string.IsNullOrWhiteSpace(_options.Authentication.GCloudExecutable)
            ? "gcloud"
            : _options.Authentication.GCloudExecutable;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start '{executable}'. Install the Google Cloud CLI or set GoogleCloudGraph:Authentication:Mode to AccessToken or ApiKey.", ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var output = (await outputTask.ConfigureAwait(false)).Trim();
        var error = (await errorTask.ConfigureAwait(false)).Trim();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            logger.LogError("gcloud token command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"gcloud token command failed. Re-authenticate with gcloud and retry. Error: {error}");
        }

        return output;
    }
}
