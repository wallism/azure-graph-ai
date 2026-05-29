namespace CloudGraphAI.Azure.Models;

public static class AzureResourceId
{
    public static string BuildSubscriptionId(string subscriptionId)
        => $"/subscriptions/{subscriptionId.Trim().Trim('/')}";

    public static string? GetSubscriptionId(string? resourceId)
        => GetSegmentAfter(resourceId, "subscriptions");

    public static string? GetResourceGroupName(string? resourceId)
        => GetSegmentAfter(resourceId, "resourceGroups");

    public static string? GetResourceGroupId(string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        var providerIndex = resourceId.IndexOf("/providers/", StringComparison.OrdinalIgnoreCase);
        return providerIndex < 0 ? null : resourceId[..providerIndex];
    }

    public static string GetLastSegment(string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return string.Empty;

        var trimmed = resourceId.Trim().Trim('/');
        var slashIndex = trimmed.LastIndexOf('/');
        return slashIndex < 0 ? trimmed : trimmed[(slashIndex + 1)..];
    }

    /// <summary>
    /// Extracts the resource type from an Azure resource ID (e.g. "Microsoft.Web/sites").
    /// </summary>
    public static string? GetResourceType(string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        var providerIndex = resourceId.IndexOf("/providers/", StringComparison.OrdinalIgnoreCase);
        if (providerIndex < 0)
            return null;

        var afterProvider = resourceId[(providerIndex + "/providers/".Length)..];
        var segments = afterProvider.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Resource type is provider/type (e.g. Microsoft.Web/sites)
        return segments.Length >= 2 ? $"{segments[0]}/{segments[1]}" : null;
    }

    public static string? GetSegmentAfter(string? resourceId, string segmentName)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return null;

        var segments = resourceId.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals(segmentName, StringComparison.OrdinalIgnoreCase))
                return segments[i + 1];
        }

        return null;
    }
}
