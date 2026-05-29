using CloudGraphAI.Azure.Models;

namespace CloudGraphAI.Azure.Import;

public interface IDisplayNameFormatter
{
    /// <summary>
    /// Formats the display name for a node by applying configured rules
    /// (prefix removal, substring removal, environment stripping, etc).
    /// </summary>
    string Format(string rawName, AzureGraphNode node);
}
