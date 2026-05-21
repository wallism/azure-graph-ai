using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureGraphAI.GoogleCloud.Models;

public sealed class GoogleCloudAssetListResult
{
    [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
    public List<GoogleCloudAsset> Assets { get; set; } = [];

    [JsonProperty("nextPageToken", NullValueHandling = NullValueHandling.Ignore)]
    public string? NextPageToken { get; set; }
}

public sealed class GoogleCloudAsset
{
    [JsonProperty("updateTime", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? UpdateTime { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("assetType", NullValueHandling = NullValueHandling.Ignore)]
    public string? AssetType { get; set; }

    [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
    public GoogleCloudAssetResource? Resource { get; set; }

    [JsonProperty("ancestors", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Ancestors { get; set; } = [];
}

public sealed class GoogleCloudAssetResource
{
    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public string? Version { get; set; }

    [JsonProperty("resourceUrl", NullValueHandling = NullValueHandling.Ignore)]
    public string? ResourceUrl { get; set; }

    [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
    public string? Parent { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Data { get; set; }

    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public string? Location { get; set; }
}
