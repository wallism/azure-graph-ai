# Azure Import

## Configuration

Configure `src/CloudGraphAI.Importer/appsettings.Development.json` or environment variables:

- `CloudGraph:Providers:Azure:Enabled` set to `true`.
- `AzureGraph:IncludedSubscriptions`, falling back to `Azure:IncludedSubscriptions`.
- `AzureGraph:Authentication:Mode` set to `AzureCli` for local `az login` auth, or `ClientSecret` for service-principal auth.
- `Azure:TenantId` optional for `AzureCli`, required for `ClientSecret`.
- `APIS:Auth:Microsoft:ClientId`.
- `APIS:Auth:Microsoft:ClientSecret`.
- `Neo4jSettings:*`.

Example:

```powershell
az login
az account set --subscription <subscription-id>
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --dry-run
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj
```

For local development the importer defaults to `AzureCli`, so it uses your current Azure CLI login. Service-principal client secret auth is still supported by setting `AzureGraph:Authentication:Mode` to `ClientSecret`.

## Resource Filtering

By default the importer collects all known Azure resource types. To import only a subset, set `AzureGraph:IncludeResources` to an explicit list of resource names:

```json
{
  "AzureGraph": {
    "IncludeResources": [ "WebApp", "VNet", "KeyVault" ]
  }
}
```

The default value is `["All"]`, which imports everything.

You can also pass this at runtime via the `--include-resources` argument (comma-separated), which overrides the appsettings value for both Azure and Google Cloud:

```powershell
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --include-resources WebApp,VNet,KeyVault
```

### Always-included resources

The following resources are always imported regardless of the filter, because they provide the structural hierarchy that other resources depend on:

- `Subscription`
- `ResourceGroup`

### Child resources

Some resources are part of a parent and are automatically included when the parent is listed:

| Child | Included when parent is listed |
|-------|-------------------------------|
| `Subnet` | `VNet` |
| `VirtualNetworkPeering` | `VNet` |
| `WebJob` | `WebApp` |

### Available resource names

Use these values in `IncludeResources` (case-insensitive):

- `VNet` (includes Subnet, VirtualNetworkPeering)
- `StorageAccount`
- `KeyVault`
- `UserAssignedManagedIdentity`
- `ServerFarm`
- `ContainerRegistry`
- `CosmosDbAccount`
- `AzureAIFoundryAccount`
- `RedisCache`
- `SqlManagedInstance`
- `ContainerApp`
- `WebApp` (includes WebJob)

## Current Collectors

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

## Loading Model

The Azure importer does not ask Azure for every resource in a resource group and then dynamically map whatever comes back. It uses explicit collectors for the Azure resource types the project knows how to model.

The import flow is:

1. Read configured subscription IDs.
2. Build an import context for those subscriptions.
3. Run every registered `IAzureResourceCollector` in `Order`.
4. Each collector calls the Azure REST endpoint for its resource type for each configured subscription.
5. Each collector follows Azure `nextLink` paging until all pages for that resource type are loaded.
6. Common links are applied to each resource node:
   - `IN_SUBSCRIPTION`
   - `IN_GROUP`, where the ARM ID contains a resource group
   - optional `environment`, from configured environment rules
7. After all nodes have been collected, collectors build cross-resource relationship IDs.
8. Dangling relationship IDs are pruned before graph writes.
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

## Relationship Enrichment

Azure AI Foundry accounts are represented by `Microsoft.CognitiveServices/accounts`. The importer saves `properties.endpoint` as both `endpoint` and `boundaryApiEndpoint` graph properties so questions can target the data-plane/API boundary endpoint directly.

Web Apps and Container Apps can be related to Azure AI Foundry accounts with `CONNECTS_TO_AI_FOUNDRY`. Web Apps use values from app settings. Container Apps use environment variable values present in the container app ARM response. The setting names are configurable under `AzureGraph:AzureAIFoundryEndpointSettingNames`.

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

## Cost Import

The importer collects per-resource cost data for the previous billing month from the Azure Cost Management API. Each resource with non-zero cost gets a `MonthResourceCost` node linked via a `COST_FOR` relationship. History builds up over time — each monthly import adds new cost nodes without overwriting previous months.

Cost nodes store:
- `cost` — total pre-tax cost (decimal, 2 d.p.)
- `currency` — e.g. "AUD", "USD"
- `billingMonth` — "YYYY-MM" format
- `resourceId`, `resourceName`, `resourceType`, `resourceGroupName`, `subscriptionId`

To exclude cost import, remove `MonthResourceCost` from `IncludeResources` (or specify an explicit list without it).

### Required Azure Roles

The Cost Management API requires the authenticated identity to have one of the following roles **at the subscription scope**:

| Role | Description |
|------|-------------|
| **Cost Management Reader** | Read-only access to cost data. Minimum required role. |
| **Cost Management Contributor** | Read + manage cost configurations. |
| **Reader** + **Cost Management Reader** | If using a limited reader identity, the explicit cost role is still needed. |
| **Contributor** / **Owner** | These include cost access implicitly. |

> **Note:** The generic `Reader` role alone is **not sufficient** to query the Cost Management API.

To assign the role for a service principal or managed identity:

```bash
az role assignment create \
  --assignee <client-id-or-object-id> \
  --role "Cost Management Reader" \
  --scope /subscriptions/<subscription-id>
```

For Azure CLI (`AzureCli` auth mode), the logged-in user must have this role on the target subscriptions.

If the required role is missing, the importer logs a warning and skips cost collection — all other resource imports continue normally.
