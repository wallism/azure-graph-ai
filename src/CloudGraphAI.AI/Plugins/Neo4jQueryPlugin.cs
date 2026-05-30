using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Neo4j.Driver;
using Neo4jLiteRepo;
using Neo4jLiteRepo.Exceptions;
using Newtonsoft.Json;

namespace CloudGraphAI.AI.Plugins;

public sealed class Neo4jQueryPlugin(
    INeo4jGenericRepo repo,
    ILogger<Neo4jQueryPlugin> logger)
{
    private static readonly Regex SafeNodeLabel = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    [KernelFunction, Description("Returns the current graph schema map as JSON, including node labels and relationships.")]
    public async Task<string> GetGraphSchemaAsync()
        => await repo.GetGraphMapAsJsonAsync().ConfigureAwait(false);

    [KernelFunction, Description("Returns property names found on nodes with a given label.")]
    public async Task<string> GetNodePropertiesAsync(
        [Description("Node label, for example WebApp, ResourceGroup, StorageAccount, VNet, Subnet, ContainerApp.")] string nodeLabel)
    {
        if (!SafeNodeLabel.IsMatch(nodeLabel))
            throw new InvalidOperationException("Invalid node label.");

        var query = $"""
                    MATCH (n:{nodeLabel})
                    RETURN keys(n) AS properties
                    LIMIT 25
                    """;

        var results = await repo.ExecuteReadListStringsAsync(query, "properties").ConfigureAwait(false);
        var properties = results
            .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        return JsonConvert.SerializeObject(properties);
    }

    [KernelFunction, Description("Executes a read-only Cypher query and returns result rows as compact JSON. Cypher has no GROUP BY clause; aggregate by returning non-aggregated expressions alongside sum/count/etc.")]
    public async Task<string> ExecuteReadOnlyCypherAsync(
        [Description("A single read-only Cypher query. Do not use SQL GROUP BY; in Cypher, RETURN a, sum(b) AS total groups by a. Writes, CALL, DELETE, CREATE, MERGE, SET, DROP and multi-statements are rejected.")] string cypher,
        [Description("Maximum rows to return. Defaults to 100 and is capped at 500.")] int maxRows = 100)
    {
        var cappedRows = Math.Clamp(maxRows, 1, 500);
        string safeQuery;
        try
        {
            safeQuery = CypherSafety.PrepareReadOnlyQuery(cypher, cappedRows);
        }
        catch (InvalidOperationException ex)
        {
            return SerializeQueryError(ex.Message);
        }

        try
        {
            logger.LogInformation("Executing read-only Cypher:{NewLine}{Cypher}", Environment.NewLine, safeQuery);

            var records = await repo.ExecuteRawReadQueryAsync(safeQuery).ConfigureAwait(false);
            var rows = records.Select(record =>
                record.Keys.ToDictionary(key => key, key => ToSerializable(record[key]))).ToList();

            return JsonConvert.SerializeObject(rows);
        }
        catch (RepositoryException ex)
        {
            logger.LogWarning(ex, "AI-generated Cypher failed. QueryLength={QueryLength}", safeQuery.Length);
            return SerializeQueryError(GetRootMessage(ex));
        }
        catch (ClientException ex)
        {
            logger.LogWarning(ex, "AI-generated Cypher failed. QueryLength={QueryLength}", safeQuery.Length);
            return SerializeQueryError(ex.Message);
        }
    }

    private static string SerializeQueryError(string message)
        => JsonConvert.SerializeObject(new
        {
            error = "cypher_query_failed",
            message,
            retryGuidance = "Revise the Cypher and call this tool again. For aggregation, Cypher has no GROUP BY clause; return grouping keys alongside aggregate expressions instead."
        });

    private static string GetRootMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }

    private static object? ToSerializable(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case INode node:
                return new
                {
                    labels = node.Labels,
                    properties = ToSerializable(node.Properties)
                };
            case IRelationship relationship:
                return new
                {
                    type = relationship.Type,
                    properties = ToSerializable(relationship.Properties)
                };
            case IReadOnlyDictionary<string, object> dictionary:
                return dictionary.ToDictionary(pair => pair.Key, pair => ToSerializable(pair.Value));
            case IDictionary<string, object> dictionary:
                return dictionary.ToDictionary(pair => pair.Key, pair => ToSerializable(pair.Value));
            case IEnumerable<object> values when value is not string:
                return values.Select(ToSerializable).ToList();
            default:
                return value;
        }
    }
}
