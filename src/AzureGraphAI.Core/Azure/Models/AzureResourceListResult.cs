using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class AzureResourceListResult<T>
    where T : AzureGraphNode
{
    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public List<T> Value { get; set; } = [];

    [JsonProperty("nextLink", NullValueHandling = NullValueHandling.Ignore)]
    public string? NextLink { get; set; }
}
