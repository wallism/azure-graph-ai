namespace CloudGraphAI.Azure.Configuration;

public sealed class DisplayNameOptions
{
    /// <summary>
    /// When true, strips the resolved environment name (e.g. "Production", "Development")
    /// from the beginning of resource names.
    /// </summary>
    public bool StripEnvironmentPrefix { get; set; }

    /// <summary>
    /// Ordered list of prefixes to strip from the beginning of resource names.
    /// Useful for removing organization or project naming conventions.
    /// Example: ["ACME", "MyOrg", "proj"]
    /// </summary>
    public List<string> PrefixesToRemove { get; set; } = [];

    /// <summary>
    /// Substrings to remove from anywhere in the resource name.
    /// Useful for stripping common naming patterns like "acme-prod-eastus-asa-".
    /// </summary>
    public List<string> SubstringsToRemove { get; set; } = [];
}
