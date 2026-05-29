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

The import services enforce unique constraints, upsert all nodes, then upsert all relationships. Dangling relationship IDs are pruned before graph writes so edges are only written when both endpoint nodes were loaded in the current import context.

## AI Console

Configure `src/CloudGraphAI.Console/appsettings.Development.json` or environment variables:

- `AI:OpenAI:ModelId`
- `AI:OpenAI:ApiKey` or `OPENAI_API_KEY`
- `Neo4jSettings:*`

Then run:

```powershell
dotnet run --project src\CloudGraphAI.Console\CloudGraphAI.Console.csproj
```

The AI console reads from Neo4j only. It does not call Azure or Google Cloud APIs at question time, so Azure CLI or gcloud authentication is not required for the console after the graph has been imported.

The AI plugin can inspect the graph schema and execute read-only Cypher. It rejects write clauses, `CALL`, and multi-statement Cypher before execution.
