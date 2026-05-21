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
dotnet run --project src\AzureGraphAI.Importer\AzureGraphAI.Importer.csproj -- --dry-run
```

## Neo4j Configuration

Configure `Neo4jSettings` in `src/AzureGraphAI.Importer/appsettings.Development.json`, `src/AzureGraphAI.Console/appsettings.Development.json`, or environment variables:

- `Neo4jSettings:ConnectionUri`
- `Neo4jSettings:User`
- `Neo4jSettings:Password`
- `Neo4jSettings:Database`

The import services enforce unique constraints, upsert all nodes, then upsert all relationships. Dangling relationship IDs are pruned before graph writes so edges are only written when both endpoint nodes were loaded in the current import context.

## AI Console

Configure `src/AzureGraphAI.Console/appsettings.Development.json` or environment variables:

- `AI:OpenAI:ModelId`
- `AI:OpenAI:ApiKey` or `OPENAI_API_KEY`
- `Neo4jSettings:*`

Then run:

```powershell
dotnet run --project src\AzureGraphAI.Console\AzureGraphAI.Console.csproj
```

The AI console reads from Neo4j only. It does not call Azure or Google Cloud APIs at question time, so Azure CLI or gcloud authentication is not required for the console after the graph has been imported.

The AI plugin can inspect the graph schema and execute read-only Cypher. It rejects write clauses, `CALL`, and multi-statement Cypher before execution.
