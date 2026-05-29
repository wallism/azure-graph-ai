using Neo4jLiteRepo;
using Neo4jLiteRepo.Attributes;
using Newtonsoft.Json;

namespace CloudGraphAI.Azure.Models;

#region API Models

public sealed class CostQueryRequest
{
    [JsonProperty("type")]
    public string Type { get; set; } = "ActualCost";

    [JsonProperty("timeframe")]
    public string Timeframe { get; set; } = "TheLastBillingMonth";

    [JsonProperty("dataset")]
    public CostQueryDataset Dataset { get; set; } = new();
}

public sealed class CostQueryDataset
{
    [JsonProperty("granularity")]
    public string Granularity { get; set; } = "None";

    [JsonProperty("aggregation")]
    public Dictionary<string, CostAggregation> Aggregation { get; set; } = new()
    {
        ["totalCost"] = new CostAggregation { Name = "PreTaxCost", Function = "Sum" }
    };

    [JsonProperty("grouping")]
    public List<CostGrouping>? Grouping { get; set; }
}

public sealed class CostAggregation
{
    [JsonProperty("name")]
    public string Name { get; set; } = "PreTaxCost";

    [JsonProperty("function")]
    public string Function { get; set; } = "Sum";
}

public sealed class CostGrouping
{
    [JsonProperty("type")]
    public string Type { get; set; } = "Dimension";

    [JsonProperty("name")]
    public string Name { get; set; } = "ResourceId";
}

public sealed class CostQueryResponse
{
    [JsonProperty("properties")]
    public CostQueryResponseProperties? Properties { get; set; }
}

public sealed class CostQueryResponseProperties
{
    [JsonProperty("columns")]
    public List<CostColumn>? Columns { get; set; }

    [JsonProperty("rows")]
    public List<List<object>>? Rows { get; set; }

    [JsonProperty("nextLink")]
    public string? NextLink { get; set; }
}

public sealed class CostColumn
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}

#endregion

#region Graph Node

/// <summary>
/// Represents the cost of a single Azure resource for a specific billing month.
/// Stored as a standalone node with COST_FOR relationships to the resource.
/// </summary>
public sealed class MonthResourceCost : AzureGraphNode
{
    [NodeProperty("resourceId")]
    [JsonIgnore]
    public string ResourceId { get; set; } = string.Empty;

    [NodeProperty("resourceName")]
    [JsonIgnore]
    public string? ResourceName { get; set; }

    [NodeProperty("resourceType")]
    [JsonIgnore]
    public string? ResourceType { get; set; }

    [NodeProperty("resourceGroupName")]
    [JsonIgnore]
    public string? ResourceGroupName { get; set; }

    [NodeProperty("subscriptionId")]
    [JsonIgnore]
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Billing month in YYYY-MM format (e.g. "2026-04") for consistent sorting and querying.
    /// </summary>
    [NodeProperty("billingMonth")]
    [JsonIgnore]
    public string BillingMonth { get; set; } = string.Empty;

    /// <summary>
    /// Total pre-tax cost for this resource in this billing month.
    /// </summary>
    [NodeProperty("cost")]
    [JsonIgnore]
    public decimal Cost { get; set; }

    /// <summary>
    /// Currency code (e.g. "USD", "AUD").
    /// </summary>
    [NodeProperty("currency")]
    [JsonIgnore]
    public string Currency { get; set; } = string.Empty;

    public override string BuildDisplayName()
        => $"{ResourceName ?? AzureResourceId.GetLastSegment(ResourceId)} ({BillingMonth})";

    public override string GetMainContent()
        => $"MonthResourceCost: {DisplayName} - {Cost:N2} {Currency}";
}

#endregion
