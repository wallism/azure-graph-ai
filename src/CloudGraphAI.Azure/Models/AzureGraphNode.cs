using Neo4jLiteRepo;
using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public abstract class AzureGraphNode : GraphNode
{
    [NodePrimaryKey]
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public required string Id { get; set; }

    [NodeProperty("azureName")]
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Name { get; set; }

    [NodeProperty("azureType")]
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Type { get; set; }

    [NodeProperty("location")]
    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Location { get; set; }

    [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string?>? Tags { get; set; }

    [NodeProperty("tagsJson")]
    [JsonIgnore]
    public string? TagsJson => Tags is { Count: > 0 }
        ? JsonConvert.SerializeObject(Tags.OrderBy(kvp => kvp.Key))
        : null;

    public override string BuildDisplayName()
        => string.IsNullOrWhiteSpace(Name) ? AzureResourceId.GetLastSegment(Id) : Name;

    public override string GetMainContent()
    {
        var parts = new[]
        {
            $"{GetType().Name}: {DisplayName}",
            string.IsNullOrWhiteSpace(Type) ? null : $"Type: {Type}",
            string.IsNullOrWhiteSpace(Location) ? null : $"Location: {Location}",
            string.IsNullOrWhiteSpace(Id) ? null : $"Id: {Id}"
        };

        return string.Join(Environment.NewLine, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    public string? GetTagValue(string key)
    {
        if (Tags is null)
            return null;

        foreach (var pair in Tags)
        {
            if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }
}
