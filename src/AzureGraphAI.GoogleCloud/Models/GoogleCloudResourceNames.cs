using System.Text.RegularExpressions;

namespace AzureGraphAI.GoogleCloud.Models;

public static partial class GoogleCloudResourceNames
{
    public static string GetLastSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? value;
    }

    public static string? GetProjectId(string? value)
        => GetPathSegment(value, "projects");

    public static string? GetLocation(string? value)
        => GetPathSegment(value, "locations")
            ?? GetPathSegment(value, "regions")
            ?? GetPathSegment(value, "zones");

    public static string? GetPathSegment(string? value, string segmentName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = Regex.Match(value, $@"(?:^|/){Regex.Escape(segmentName)}/([^/?#]+)", RegexOptions.IgnoreCase);
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }

    public static string BuildCloudResourceManagerName(string relativeName)
        => relativeName.StartsWith("//", StringComparison.Ordinal)
            ? relativeName
            : $"//cloudresourcemanager.googleapis.com/{relativeName.TrimStart('/')}";

    public static string? FromComputeSelfLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        const string computeRestPrefix = "https://www.googleapis.com/compute/v1/";
        const string computeApiPrefix = "https://compute.googleapis.com/compute/v1/";

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
            return trimmed;

        if (trimmed.StartsWith(computeRestPrefix, StringComparison.OrdinalIgnoreCase))
            return $"//compute.googleapis.com/{trimmed[computeRestPrefix.Length..]}";

        if (trimmed.StartsWith(computeApiPrefix, StringComparison.OrdinalIgnoreCase))
            return $"//compute.googleapis.com/{trimmed[computeApiPrefix.Length..]}";

        if (trimmed.StartsWith("projects/", StringComparison.OrdinalIgnoreCase))
            return $"//compute.googleapis.com/{trimmed}";

        return trimmed;
    }

    public static bool IsProjectReference(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && ProjectReferenceRegex().IsMatch(value);

    [GeneratedRegex(@"^projects/[^/]+$", RegexOptions.IgnoreCase)]
    private static partial Regex ProjectReferenceRegex();
}
