using Neo4jLiteRepo;
using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.GoogleCloud.Models;

public abstract class GoogleCloudGraphNode : GraphNode
{
    [NodePrimaryKey]
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public required string Id { get; set; }

    [NodeProperty("provider")]
    [JsonIgnore]
    public string Provider => "GoogleCloud";

    [NodeProperty("gcpName")]
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Name { get; set; }

    [NodeProperty("gcpAssetType")]
    [JsonProperty("assetType", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Type { get; set; }

    [NodeProperty("location")]
    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public virtual string? Location { get; set; }

    [NodeProperty("resourceUrl")]
    [JsonProperty("resourceUrl", NullValueHandling = NullValueHandling.Ignore)]
    public string? ResourceUrl { get; set; }

    [NodeProperty("parent")]
    [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
    public string? Parent { get; set; }

    [NodeProperty("updateTime")]
    [JsonIgnore]
    public string? UpdateTimeValue => UpdateTime?.ToString("O");

    [JsonProperty("updateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? UpdateTime { get; set; }

    [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string?>? Labels { get; set; }

    [NodeProperty("labelsJson")]
    [JsonIgnore]
    public string? LabelsJson => Labels is { Count: > 0 }
        ? JsonConvert.SerializeObject(Labels.OrderBy(kvp => kvp.Key))
        : null;

    public override string BuildDisplayName()
        => string.IsNullOrWhiteSpace(Name) ? GoogleCloudResourceNames.GetLastSegment(Id) : Name;

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
}
