using System.Net;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CloudGraphAI.Azure.Import;

/// <summary>
/// Collects per-resource cost data for the previous billing month from Azure Cost Management API.
/// Creates MonthResourceCost nodes linked to the corresponding resources via COST_FOR relationships.
/// </summary>
public sealed class ResourceCostCollector(
    IAzureRestApi azureApi,
    ILogger<ResourceCostCollector> logger)
    : IAzureResourceCollector
{
    /// <summary>
    /// Runs after all resource collectors have populated the context, so we can resolve resource names.
    /// </summary>
    public int Order => 200;

    public string Name => nameof(MonthResourceCost);

    public async Task<IReadOnlyList<AzureGraphNode>> CollectAsync(AzureImportContext context, CancellationToken cancellationToken = default)
    {
        var costNodes = new List<MonthResourceCost>();
        var billingMonth = DateTime.Now.AddMonths(-1).ToString("yyyy-MM");

        var request = new CostQueryRequest
        {
            Type = "ActualCost",
            Timeframe = "TheLastBillingMonth",
            Dataset = new CostQueryDataset
            {
                Granularity = "None",
                Aggregation = new Dictionary<string, CostAggregation>
                {
                    ["totalCost"] = new() { Name = "PreTaxCost", Function = "Sum" }
                },
                Grouping = [new CostGrouping { Type = "Dimension", Name = "ResourceId" }]
            }
        };

        foreach (var subscriptionId in context.SubscriptionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Querying cost data for subscription {SubscriptionId}, billing month {BillingMonth}",
                subscriptionId, billingMonth);

            var result = await azureApi.PostCostManagementQueryAsync(subscriptionId, request, cancellationToken)
                .ConfigureAwait(false);

            if (result.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning(
                    "Cost import skipped for subscription {SubscriptionId}: access denied (HTTP {StatusCode}). " +
                    "The authenticated identity requires the 'Cost Management Reader' role (or higher) " +
                    "at the subscription scope to retrieve cost data. " +
                    "Assign the role via: az role assignment create --assignee <principalId> --role 'Cost Management Reader' --scope /subscriptions/{SubscriptionId}",
                    subscriptionId, (int)result.StatusCode);
                continue;
            }

            if (!result.WasSuccessful || result.Value?.Properties?.Rows is null)
            {
                logger.LogWarning("Failed to retrieve cost data for subscription {SubscriptionId}: {Error}",
                    subscriptionId, result.Exception?.Message ?? result.RawText ?? "No response");
                continue;
            }

            var parsed = ParseCostResponse(result.Value, subscriptionId, billingMonth, context);
            costNodes.AddRange(parsed);

            logger.LogInformation("Collected {Count} resource cost entries for subscription {SubscriptionId}",
                parsed.Count, subscriptionId);
        }

        context.AddNodes(costNodes);
        return costNodes;
    }

    public Task BuildRelationshipsAsync(AzureImportContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    private List<MonthResourceCost> ParseCostResponse(
        CostQueryResponse response,
        string subscriptionId,
        string billingMonth,
        AzureImportContext context)
    {
        var columns = response.Properties!.Columns!;
        var rows = response.Properties!.Rows!;

        var costIndex = columns.FindIndex(c => c.Name.Equals("PreTaxCost", StringComparison.OrdinalIgnoreCase));
        var resourceIdIndex = columns.FindIndex(c => c.Name.Equals("ResourceId", StringComparison.OrdinalIgnoreCase));
        var currencyIndex = columns.FindIndex(c => c.Name.Equals("Currency", StringComparison.OrdinalIgnoreCase));

        if (costIndex < 0 || resourceIdIndex < 0)
        {
            logger.LogWarning("Cost response missing expected columns (PreTaxCost, ResourceId). Columns: {Columns}",
                string.Join(", ", columns.Select(c => c.Name)));
            return [];
        }

        var results = new List<MonthResourceCost>();

        foreach (var row in rows)
        {
            if (row.Count <= Math.Max(costIndex, Math.Max(resourceIdIndex, currencyIndex)))
                continue;

            var resourceId = row[resourceIdIndex]?.ToString();
            if (string.IsNullOrWhiteSpace(resourceId))
                continue;

            var cost = ParseDecimal(row[costIndex]);
            if (cost <= 0)
                continue;

            var currency = currencyIndex >= 0 ? row[currencyIndex]?.ToString() ?? "USD" : "USD";
            var resourceName = AzureResourceId.GetLastSegment(resourceId);
            var resourceType = AzureResourceId.GetResourceType(resourceId);
            var resourceGroupName = AzureResourceId.GetResourceGroupName(resourceId);

            // Try to resolve a friendly name from context
            var existingNode = context.GetAllNodes()
                .FirstOrDefault(n => n.Id.Equals(resourceId, StringComparison.OrdinalIgnoreCase));
            if (existingNode?.Name is not null)
                resourceName = existingNode.Name;

            results.Add(new MonthResourceCost
            {
                Id = $"{resourceId}|{billingMonth}",
                ResourceId = resourceId,
                ResourceName = resourceName,
                ResourceType = resourceType,
                ResourceGroupName = resourceGroupName,
                SubscriptionId = subscriptionId,
                BillingMonth = billingMonth,
                Cost = Math.Round(cost, 2),
                Currency = currency
            });
        }

        return results;
    }

    private static decimal ParseDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            long l => l,
            int i => i,
            JValue jv => jv.Type == JTokenType.Float ? (decimal)jv.Value<double>() : jv.Value<decimal>(),
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => 0m
        };
    }
}
