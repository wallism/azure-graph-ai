using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class WebJob : AzureResourceNode
{
    [NodeProperty("")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public WebJobProperties? Properties { get; set; }

    [NodeRelationship<WebApp>("RUNS_IN_WEBAPP")]
    public List<string> WebApps { get; set; } = [];

    public override string BuildDisplayName()
        => Properties?.Name ?? Name ?? base.BuildDisplayName();
}

public sealed class WebJobProperties
{
    [NodeProperty("webJobName")]
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [NodeProperty("status")]
    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public string? Status { get; set; }

    [NodeProperty("runCommand")]
    [JsonProperty("runCommand", NullValueHandling = NullValueHandling.Ignore)]
    public string? RunCommand { get; set; }
}
