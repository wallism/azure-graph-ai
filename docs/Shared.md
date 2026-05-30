# Neo4j and AI Console

## Provider Selection

The importer can run Azure, Google Cloud, or both in the same execution:

```json
{
  "CloudGraph": {
    "Providers": {
      "Azure": { "Enabled": true },
      "GoogleCloud": { "Enabled": true }
    }
  }
}
```

Each provider has its own models and collectors, but both write to the same Neo4j database through `Neo4jLiteRepo`.

The dry run verifies Neo4j connectivity and then performs provider-specific read checks without writing graph nodes or relationships:

```powershell
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --dry-run
```

## Resource Filtering

Both providers support an `IncludeResources` configuration array to limit which resource types are imported. The default is `["All"]`.

To override at runtime, pass `--include-resources` with a comma-separated list:

```powershell
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --include-resources WebApp,VNet,KeyVault
```

This overrides the appsettings value for both Azure and Google Cloud providers simultaneously. Standard .NET command-line configuration syntax also works for provider-specific overrides:

```powershell
dotnet run -- --AzureGraph:IncludeResources:0=WebApp --AzureGraph:IncludeResources:1=VNet
```

Structural resources (Subscription/ResourceGroup for Azure, Organization/Folder/Project for Google Cloud) are always imported. Child resources that are part of a parent (e.g. Subnet with VNet, Subnetwork with Network) are automatically included when the parent is listed.

See [Azure.md](Azure.md#resource-filtering) and [GoogleCloud.md](GoogleCloud.md#resource-filtering) for the full list of available resource names per provider.

## Neo4j Configuration

Configure `Neo4jSettings` in `src/CloudGraphAI.Importer/appsettings.Development.json`, `src/CloudGraphAI.Console/appsettings.Development.json`, or environment variables:

- `Neo4jSettings:ConnectionUri`
- `Neo4jSettings:User`
- `Neo4jSettings:Password`
- `Neo4jSettings:Database`

### Automatic Database Creation

On startup the importer checks whether the configured database exists and is online. Behaviour depends on the Neo4j edition:

| Edition | Behaviour |
|---------|-----------|
| **Enterprise / Aura Enterprise** | Automatically creates the database with `CREATE DATABASE ... IF NOT EXISTS` and waits (up to 30 s) for it to come online. |
| **Aura Free / Professional** | Reports as enterprise but may lack `CREATE DATABASE` privileges. If creation fails the importer logs the error with manual steps and exits. Use the pre-provisioned database name (usually `neo4j`) or create the database via the Aura Console. |
| **Community Edition** | Only the default `neo4j` database is supported. The importer logs a message explaining the limitation and exits. Set `Neo4jSettings:Database` to `neo4j` (or leave it empty) to use the default database. |

If you need to create the database manually, connect to the system database and run:

```cypher
:use system
CREATE DATABASE `cloud-graph-ai` IF NOT EXISTS;
```

You can verify the database is online with:

```cypher
SHOW DATABASE `cloud-graph-ai` YIELD name, currentStatus;
```

The import services enforce unique constraints, upsert all nodes, then upsert all relationships. Relationship writes are skipped when the target node was not loaded in the current import context, so partial imports are additive and do not delete existing graph relationships.

## AI Console

Configure `src/CloudGraphAI.Console/appsettings.Development.json` or environment variables:

- `Neo4jSettings:*`
- `AIModels:DefaultChatServiceId` optional; when empty, the first enabled chat deployment is used.
- `AIModels:AzureFoundry:*`, `AIModels:GoogleVertexAI:*`, or `AIModels:AwsBedrock:*`.

Only one AI provider needs to be enabled for the console to work. Each deployment has a globally unique `ServiceId`; `DefaultChatServiceId` must match one configured chat deployment when set.

Example using Google Vertex AI:

```json
{
  "AIModels": {
    "DefaultChatServiceId": "gemini-3.5-flash",
    "GoogleVertexAI": {
      "Enabled": true,
      "ProjectId": "my-gcp-project",
      "Location": "global",
      "CredentialsPath": "C:\\path\\to\\service-account.json",
      "Deployments": [
        {
          "ServiceId": "gemini-3.5-flash",
          "ModelId": "gemini-3.5-flash",
          "Type": "Chat"
        }
      ]
    }
  }
}
```

Example using Azure Foundry:

```json
{
  "AIModels": {
    "AzureFoundry": {
      "Enabled": true,
      "Endpoint": "https://my-foundry.services.ai.azure.com/",
      "ApiKey": "",
      "Deployments": [
        {
          "ServiceId": "azure-foundry-chat",
          "DeploymentName": "my-chat-deployment",
          "Type": "Chat"
        }
      ]
    }
  }
}
```

Example using AWS Bedrock:

```json
{
  "AIModels": {
    "AwsBedrock": {
      "Enabled": true,
      "Region": "us-east-1",
      "AccessKeyId": "",
      "SecretAccessKey": "",
      "Deployments": [
        {
          "ServiceId": "bedrock-chat",
          "ModelId": "anthropic.claude-sonnet-4-20250514-v1:0",
          "Type": "Chat"
        }
      ]
    }
  }
}
```

For local development, keep provider keys and service-account paths in `appsettings.Development.json`, user secrets, or environment variables. Do not put real provider secrets in `appsettings.json`.

Then run:

```powershell
dotnet run --project src\CloudGraphAI.Console\CloudGraphAI.Console.csproj
```

The AI console reads cloud-resource data from Neo4j only. It does not call Azure or Google Cloud resource APIs at question time, so Azure CLI or gcloud authentication is not required for graph reads after the graph has been imported. The configured AI provider is still called for chat completion.

The AI plugin can inspect the graph schema and execute read-only Cypher. It rejects write clauses, `CALL`, multi-statement Cypher, and SQL-style `GROUP BY` before execution. Cypher aggregation is implicit: return grouping keys beside aggregate expressions, for example:

```cypher
MATCH (m:MonthResourceCost)
WHERE m.resourceGroupName IS NOT NULL
RETURN m.resourceGroupName AS resourceGroupName, m.currency AS currency, sum(m.cost) AS totalCost
ORDER BY totalCost DESC
LIMIT 10
```

If an AI-generated query fails, the Neo4j tool returns a structured `cypher_query_failed` result so the model can revise and retry instead of crashing the console.
