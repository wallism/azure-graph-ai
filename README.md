# Cloud Graph AI

Graph cloud resources in Neo4j, then query that graph with an AI console.

Open `src\CloudGraphAI.sln` in Visual Studio.

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
