using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Neo4j.Driver;
using Neo4jLiteRepo;
using Newtonsoft.Json;

namespace AzureGraphAI.AI.Plugins;

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

    [KernelFunction, Description("Executes a read-only Cypher query and returns result rows as compact JSON. Use this after creating a MATCH/RETURN query.")]
    public async Task<string> ExecuteReadOnlyCypherAsync(
        [Description("A single read-only Cypher query. Writes, CALL, DELETE, CREATE, MERGE, SET, DROP and multi-statements are rejected.")] string cypher,
        [Description("Maximum rows to return. Defaults to 100 and is capped at 500.")] int maxRows = 100)
    {
        var cappedRows = Math.Clamp(maxRows, 1, 500);
        var safeQuery = CypherSafety.PrepareReadOnlyQuery(cypher, cappedRows);
        logger.LogInformation("Executing AI-generated read-only Cypher: {Cypher}", safeQuery);

        var records = await repo.ExecuteRawReadQueryAsync(safeQuery).ConfigureAwait(false);
        var rows = records.Select(record =>
            record.Keys.ToDictionary(key => key, key => ToSerializable(record[key]))).ToList();

        return JsonConvert.SerializeObject(rows);
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
