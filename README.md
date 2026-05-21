# azure-graph-ai

Graph Azure resources in Neo4j, then query that graph with an AI console.

Open `src\AzureGraphAI.sln` in Visual Studio.

## Projects

- `AzureGraphAI.Core`: Azure resource models, Azure REST API clients, collectors, and Neo4j import orchestration.
- `AzureGraphAI.Importer`: console importer for configured Azure subscriptions.
- `AzureGraphAI.AI`: Semantic Kernel chat runner plus guarded Neo4j query tools.
- `AzureGraphAI.Console`: console app for asking questions about the loaded graph.
- `Neo4jLiteRepo`: Neo4j access layer. This repo uses the newer copy already in `src`.
- `Agile.API.Clients`: reusable HTTP/rate-limit/retry client library.

## Importer

Configure `src/AzureGraphAI.Importer/appsettings.Development.json` or environment variables:

- `Azure:TenantId`
- `AzureGraph:IncludedSubscriptions`
- `APIS:Auth:Microsoft:ClientId`
- `APIS:Auth:Microsoft:ClientSecret`
- `Neo4jSettings:ConnectionUri`
- `Neo4jSettings:User`
- `Neo4jSettings:Password`

Then run:

```powershell
dotnet run --project src\AzureGraphAI.Importer\AzureGraphAI.Importer.csproj
```

The current collectors cover subscriptions, resource groups, VNets, subnets, peerings, storage accounts, key vaults, app service plans, web apps, web jobs, container registries, container apps, Redis caches, and SQL managed instances.

## Environments

Environment is intentionally an optional enrichment, not a core Azure resource concept. Configure project-specific rules under `AzureGraph:EnvironmentRules`; matching resources get an `environment` graph property. If no rule matches, no environment value is written.

## AI Console

Configure `src/AzureGraphAI.Console/appsettings.Development.json` or environment variables:

- `AI:OpenAI:ModelId`
- `AI:OpenAI:ApiKey` or `OPENAI_API_KEY`
- `Neo4jSettings:*`

Then run:

```powershell
dotnet run --project src\AzureGraphAI.Console\AzureGraphAI.Console.csproj
```

The AI plugin can inspect the graph schema and execute read-only Cypher. It rejects write clauses, `CALL`, and multi-statement Cypher before execution.
