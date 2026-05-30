# Cloud Graph AI

Graph cloud resources in Neo4j, then query that graph with an AI console.

Example question:

> Show me the top 5 most expensive resource groups.

Sample answer:

### Top 5 Most Expensive Resource Groups

| Rank | Resource Group | Subscription | Location | Total Cost (USD) | Primary Cost Driver |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | `prod-web-rg` | Production | East US | $82.15 | App Service Plan: `prod-web-plan` ($78.40) |
| 2 | `analytics-rg` | Production | East US | $64.20 | SQL Database: `analytics-db` ($41.85) |
| 3 | `integration-rg` | Shared Services | West US | $51.75 | Storage Account: `integrationstore` ($29.10) |
| 4 | `training-apps-rg` | Training | Australia East | $44.30 | App Service Plan: `training-app-plan` ($39.95) |
| 5 | `dev-tools-rg` | Development | West Europe | $37.60 | Container Registry: `devregistry` ($18.25) |

---
How does it work? It works for Azure and Google Cloud but for the example let's use Azure...
Connect to Azure, specify the subscriptionID's to import, run the importer. 
Then switch to the Console app (advanced UI tech) and ask what you want to know about your resources, e.g.
- Which WebApps connect to this KeyVault?
- Give me a count of the different farm types
- Which WebApps do not enforce TLS1.2?

## Projects

- `CloudGraphAI.Azure`: Azure resource models, Azure REST API clients, collectors, and Azure import orchestration.
- `CloudGraphAI.GoogleCloud`: Google Cloud resource models, Cloud Asset Inventory client, collectors, and Google Cloud import orchestration.
- `CloudGraphAI.Importer`: console importer for enabled cloud providers.
- `CloudGraphAI.AI`: Semantic Kernel chat runner plus guarded Neo4j query tools.
- `CloudGraphAI.Console`: console app for asking questions about the loaded graph.
- `Neo4jLiteRepo`: Neo4j access layer. This repo uses the newer copy already in `src`.
- `Agile.API.Clients`: reusable HTTP/rate-limit/retry client library.

## Documentation

- [Azure import](docs/Azure.md)
- [Google Cloud import](docs/GoogleCloud.md)
- [Neo4j and AI console](docs/Shared.md)

## Quick Start

Configure `src/CloudGraphAI.Importer/appsettings.Development.json` or environment variables, then enable one or both providers:

```json
{
  "CloudGraph": {
    "Providers": {
      "Azure": { "Enabled": true },
      "GoogleCloud": { "Enabled": false }
    }
  }
}
```

Run a dry run before importing:

```powershell
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --dry-run
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj
```

Then query the loaded graph:

```powershell
dotnet run --project src\CloudGraphAI.Console\CloudGraphAI.Console.csproj
```

The AI console is configured through `AIModels` and supports Azure Foundry, Google Vertex AI, and AWS Bedrock. Enable one provider in `src\CloudGraphAI.Console\appsettings.Development.json`; see [Neo4j and AI console](docs/Shared.md#ai-console) for provider examples and Cypher query rules.
