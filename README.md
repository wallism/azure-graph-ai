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

- `AzureGraph:IncludedSubscriptions`
- `AzureGraph:Authentication:Mode` set to `AzureCli` for local `az login` auth, or `ClientSecret` for service-principal auth.
- `Azure:TenantId` optional for `AzureCli`, required for `ClientSecret`.
- `APIS:Auth:Microsoft:ClientId`
- `APIS:Auth:Microsoft:ClientSecret`
- `Neo4jSettings:ConnectionUri`
- `Neo4jSettings:User`
- `Neo4jSettings:Password`

Then run:

```powershell
az login
az account set --subscription 38ae1d66-30e9-4ad4-b432-f0386f658e97
dotnet run --project src\AzureGraphAI.Importer\AzureGraphAI.Importer.csproj -- --dry-run
dotnet run --project src\AzureGraphAI.Importer\AzureGraphAI.Importer.csproj
```

For local development the importer defaults to `AzureCli`, so it uses your current Azure CLI login. Service-principal client secret auth is still supported by setting `AzureGraph:Authentication:Mode` to `ClientSecret`.

The dry run verifies Neo4j connectivity and loads the configured Azure subscription resource without writing graph nodes or relationships.

The current collectors cover:

- subscriptions
- resource groups
- VNets
- subnets
- virtual network peerings
- storage accounts
- key vaults
- user-assigned managed identities
- app service plans
- web apps
- web jobs
- container registries
- container apps
- Cosmos DB accounts
- Azure AI Foundry accounts
- Redis caches
- SQL managed instances

### How Resource Loading Works

The importer does not ask Azure for every resource in a resource group and then dynamically map whatever comes back. It uses explicit collectors for the Azure resource types the project knows how to model.

The import flow is:

1. Read configured subscription IDs from `AzureGraph:IncludedSubscriptions`, falling back to `Azure:IncludedSubscriptions`.
2. Build an import context for those subscriptions.
3. Run every registered `IAzureResourceCollector` in `Order`.
4. Each collector calls the Azure REST endpoint for its resource type for each configured subscription.
5. Each collector follows Azure `nextLink` paging until all pages for that resource type are loaded.
6. Common links are applied to each resource node:
   - `IN_SUBSCRIPTION`
   - `IN_GROUP`, where the ARM ID contains a resource group
   - optional `environment`, from configured environment rules
7. After all nodes have been collected, collectors build cross-resource relationship IDs.
8. Dangling relationship IDs are pruned before graph writes, so edges are only written when both endpoint nodes exist in the import context.
9. Neo4j unique constraints are enforced.
10. All nodes are upserted.
11. All relationships are upserted.

Most top-level resource collectors use subscription-scope provider list APIs, for example:

- `/{subscriptionId}/resourceGroups`
- `/{subscriptionId}/providers/Microsoft.Storage/storageAccounts`
- `/{subscriptionId}/providers/Microsoft.KeyVault/vaults`
- `/{subscriptionId}/providers/Microsoft.ManagedIdentity/userAssignedIdentities`
- `/{subscriptionId}/providers/Microsoft.Web/sites`
- `/{subscriptionId}/providers/Microsoft.Web/serverfarms`
- `/{subscriptionId}/providers/Microsoft.Network/virtualNetworks`
- `/{subscriptionId}/providers/Microsoft.App/containerApps`
- `/{subscriptionId}/providers/Microsoft.ContainerRegistry/registries`
- `/{subscriptionId}/providers/Microsoft.DocumentDB/databaseAccounts`
- `/{subscriptionId}/providers/Microsoft.CognitiveServices/accounts`
- `/{subscriptionId}/providers/Microsoft.Cache/redis`
- `/{subscriptionId}/providers/Microsoft.Sql/managedInstances`

Resource groups are loaded as graph nodes and as relationship targets. They are not currently used as the outer loop for discovering resources.

Some child or detail resources are loaded differently:

- VNets include subnets and peerings in the VNet response. The VNet collector extracts those nested resources into their own graph nodes.
- Web apps are loaded from the subscription-level sites endpoint, then enriched with resource-group-scoped calls for site config, app settings, connection strings, and Windows continuous WebJobs.
- Container apps are loaded from the subscription-level Container Apps endpoint, then related to registries from their registry configuration.

This means adding support for a new Azure resource type is deliberate: add a model, add an API method, add/register a collector, and add relationship-building logic where that resource references or is referenced by other resources.

Azure AI Foundry accounts are represented by `Microsoft.CognitiveServices/accounts`. The importer saves `properties.endpoint` as both `endpoint` and `boundaryApiEndpoint` graph properties so questions can target the data-plane/API boundary endpoint directly.

Web Apps and Container Apps can be related to Azure AI Foundry accounts with `CONNECTS_TO_AI_FOUNDRY`. Web Apps use values from app settings. Container Apps use environment variable values present in the container app ARM response. The setting names are configurable under `AzureGraph:AzureAIFoundryEndpointSettingNames`; the default starting list is:

- `AzureAIFoundry:Endpoint`
- `AzureAIFoundry__Endpoint`
- `AzureAI:FoundryEndpoint`
- `FoundryEndpoint`

Web Apps can also be related to Key Vaults with `CONNECTS_TO_KEYVAULT`. The importer checks configured app setting names under `AzureGraph:KeyVaultReferenceSettingNames`, then matches the setting values against loaded Key Vault resource names and `properties.vaultUri` values. The default starting list is:

- `keyVaultBaseUrl`
- `KeyVaultBaseUrl`
- `KeyVaultUri`
- `KeyVaultUrl`
- `vaultEndPoint`
- `VaultEndpoint`
- `keyVaultPrefix`

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
