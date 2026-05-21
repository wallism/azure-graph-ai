using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

public sealed class ResourceGroup : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public ResourceGroupProperties? Properties { get; set; }

    public override string BuildDisplayName()
        => Name ?? ResourceGroupName ?? base.BuildDisplayName();
}

public sealed class ResourceGroupProperties
{
    [NodeProperty("provisioningState")]
    [JsonProperty("provisioningState", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProvisioningState { get; set; }
}
