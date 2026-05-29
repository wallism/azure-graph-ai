using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace CloudGraphAI.Azure.Infrastructure;

public sealed record AzureCliAccount(string? User, string? SubscriptionId, string? SubscriptionName);

public static class AzureCliIdentity
{
    public static async Task<AzureCliAccount> GetCurrentAccountAsync(string? tenantId = null)
    {
        var user = await GetCurrentUserAsync(tenantId);
        var (subscriptionId, subscriptionName) = GetDefaultSubscription();
        return new AzureCliAccount(user, subscriptionId, subscriptionName);
    }

    public static async Task<string?> GetCurrentUserAsync(string? tenantId = null)
    {
        try
        {
            var options = new AzureCliCredentialOptions();
            if (!string.IsNullOrWhiteSpace(tenantId))
                options.TenantId = tenantId;

            var credential = new AzureCliCredential(options);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]));

            return ExtractUsernameFromJwt(token.Token);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractUsernameFromJwt(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return null;

        var payload = parts[1];
        // Pad base64url to standard base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Try common claims for user identity
        if (root.TryGetProperty("upn", out var upn))
            return upn.GetString();
        if (root.TryGetProperty("unique_name", out var uniqueName))
            return uniqueName.GetString();
        if (root.TryGetProperty("preferred_username", out var preferred))
            return preferred.GetString();
        if (root.TryGetProperty("email", out var email))
            return email.GetString();

        return null;
    }

    private static (string? Id, string? Name) GetDefaultSubscription()
    {
        try
        {
            var profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".azure", "azureProfile.json");

            if (!File.Exists(profilePath))
                return (null, null);

            var json = File.ReadAllText(profilePath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("subscriptions", out var subscriptions))
                return (null, null);

            foreach (var sub in subscriptions.EnumerateArray())
            {
                if (sub.TryGetProperty("isDefault", out var isDefault) && isDefault.GetBoolean())
                {
                    var id = sub.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var name = sub.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    return (id, name);
                }
            }

            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }
}
