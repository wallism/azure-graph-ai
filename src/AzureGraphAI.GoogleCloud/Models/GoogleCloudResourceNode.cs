using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace AzureGraphAI.GoogleCloud.Models;

public abstract class GoogleCloudResourceNode : GoogleCloudGraphNode
{
    [NodeRelationship<GoogleProject>(CommonEdges.InProject)]
    public List<string> Projects { get; set; } = [];

    [NodeRelationship<GoogleFolder>(CommonEdges.InFolder)]
    public List<string> Folders { get; set; } = [];

    [NodeRelationship<GoogleOrganization>(CommonEdges.InOrganization)]
    public List<string> Organizations { get; set; } = [];

    [NodeProperty("projectId")]
    [JsonProperty("projectId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProjectId { get; set; }

    [NodeProperty("projectNumber")]
    [JsonProperty("projectNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProjectNumber { get; set; }

    [NodeProperty("environment")]
    [JsonProperty("environment", NullValueHandling = NullValueHandling.Ignore)]
    public string? Environment { get; set; }

    public static class CommonEdges
    {
        public const string InProject = "IN_PROJECT";
        public const string InFolder = "IN_FOLDER";
        public const string InOrganization = "IN_ORGANIZATION";
    }
}
