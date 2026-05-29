# Google Cloud Import

## Configuration

Configure `src/CloudGraphAI.Importer/appsettings.Development.json` or environment variables:

- `CloudGraph:Providers:GoogleCloud:Enabled` set to `true`.
- `GoogleCloudGraph:IncludedScopes`, using values like `projects/my-project`, `folders/123456789`, or `organizations/123456789`.
- Or use the convenience arrays:
  - `GoogleCloudGraph:IncludedProjects`
  - `GoogleCloudGraph:IncludedFolders`
  - `GoogleCloudGraph:IncludedOrganizations`
- `GoogleCloudGraph:Authentication:Mode`.
- `Neo4jSettings:*`.

Example:

```json
{
  "CloudGraph": {
    "Providers": {
      "Azure": { "Enabled": false },
      "GoogleCloud": { "Enabled": true }
    }
  },
  "GoogleCloudGraph": {
    "IncludedProjects": [ "my-project-id" ],
    "Authentication": {
      "Mode": "GCloudCli",
      "GCloudExecutable": "gcloud",
      "QuotaProject": ""
    }
  }
}
```

Run:

```powershell
gcloud auth login
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --dry-run
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj
```

No Google Cloud resources are created by the importer. It reads Cloud Asset Inventory metadata and writes only to Neo4j.

## Authentication Modes

`GCloudCli` is the default. It shells out to:

```powershell
gcloud auth print-access-token
```

`ApplicationDefaultCredentials` shells out to:

```powershell
gcloud auth application-default print-access-token
```

`AccessToken` uses `GoogleCloudGraph:Authentication:AccessToken` directly.

`ApiKey` appends `GoogleCloudGraph:Authentication:ApiKey` as the `key` query parameter. This mode is available in the client for Google APIs that permit API-key access, but it is not expected to work for private Cloud Asset Inventory imports because Cloud Asset Inventory requires IAM permissions on the requested scope.

`QuotaProject` is optional. When set, requests include the `x-goog-user-project` header, which can be useful for user credentials that need an explicit quota or billing project.

## Google Cloud Requirements

The importer does not create or enable anything in Google Cloud. Before import, the account used by `gcloud` needs:

- Cloud Asset Inventory API available for the calling project.
- Permission to call Cloud Asset Inventory for the requested project, folder, or organization scope.
- Baseline roles commonly used for metadata reads:
  - `roles/cloudasset.viewer`
  - `roles/serviceusage.serviceUsageConsumer`

If the API is disabled, IAM is missing, or the `gcloud` token is expired, the dry run reports the failing scope and the Google API error.

## Resource Filtering

By default the importer collects all known Google Cloud resource types. To import only a subset, set `GoogleCloudGraph:IncludeResources` to an explicit list of resource names:

```json
{
  "GoogleCloudGraph": {
    "IncludeResources": [ "GoogleCloudRunService", "GoogleNetwork" ]
  }
}
```

The default value is `["All"]`, which imports everything.

You can also pass this at runtime via the `--include-resources` argument (comma-separated), which overrides the appsettings value for both Azure and Google Cloud:

```powershell
dotnet run --project src\CloudGraphAI.Importer\CloudGraphAI.Importer.csproj -- --include-resources GoogleCloudRunService,GoogleNetwork
```

### Always-included resources

The following resources are always imported regardless of the filter, because they provide the structural hierarchy that other resources depend on:

- `GoogleOrganization`
- `GoogleFolder`
- `GoogleProject`

### Child resources

Some resources are part of a parent and are automatically included when the parent is listed:

| Child | Included when parent is listed |
|-------|-------------------------------|
| `GoogleSubnetwork` | `GoogleNetwork` |

### Available resource names

Use these values in `IncludeResources` (case-insensitive):

- `GoogleNetwork` (includes GoogleSubnetwork)
- `GoogleServiceAccount`
- `GoogleArtifactRepository`
- `GoogleSecret`
- `GoogleStorageBucket`
- `GoogleCloudSqlInstance`
- `GoogleVertexAiEndpoint`
- `GoogleMemorystoreRedisInstance`
- `GoogleCloudRunService`

## Current Collectors

- organizations
- folders
- projects
- Compute Engine networks
- Compute Engine subnetworks
- service accounts
- Artifact Registry repositories
- Secret Manager secrets
- Cloud Storage buckets
- Cloud SQL instances
- Vertex AI endpoints
- Memorystore for Redis instances
- Cloud Run services

## Loading Model

The Google Cloud importer uses Cloud Asset Inventory as the broad resource inventory source. Each collector requests one or more explicit asset types with `contentType=RESOURCE`, follows `nextPageToken` paging, maps the returned resource payload into typed graph nodes, and then builds relationships from known fields.

The importer intentionally does not depend on Cloud Asset Inventory `RELATIONSHIP` content. That relationship content type has additional product-tier requirements, so the importer builds first-class relationships from resource metadata instead.

Common links are applied to each resource node:

- `IN_PROJECT`
- `IN_FOLDER`
- `IN_ORGANIZATION`
- optional `environment`, from configured string rules in `GoogleCloudGraph:EnvironmentRules`

## Relationship Enrichment

Cloud Run services can be related to:

- Artifact Registry repositories with `PULLS_FROM_REGISTRY`, using container image references like `us-central1-docker.pkg.dev/project/repository/image:tag`.
- Secret Manager secrets with `USES_SECRET`, using Cloud Run secret references.
- service accounts with `RUNS_AS_SERVICE_ACCOUNT`, using the service template account email.
- Vertex AI endpoints with `CONNECTS_TO_VERTEX_AI`, using configured environment variable names under `GoogleCloudGraph:VertexAiEndpointSettingNames`.

Subnetworks, Cloud SQL instances, and Redis instances can be related to Compute Engine networks when their resource metadata includes network self links.

Vertex AI endpoints store a `boundaryApiEndpoint` graph property derived from the endpoint resource name and location, for example `https://us-central1-aiplatform.googleapis.com/v1/projects/.../locations/.../endpoints/...:predict`.

## Official References

- [Cloud Asset Inventory overview](https://docs.cloud.google.com/asset-inventory/docs/asset-inventory-overview)
- [Cloud Asset Inventory assets.list](https://docs.cloud.google.com/asset-inventory/docs/reference/rest/v1/assets/list)
- [Cloud Asset Inventory searchAllResources](https://docs.cloud.google.com/asset-inventory/docs/reference/rest/v1/TopLevel/searchAllResources)
- [Cloud Asset Inventory asset types](https://docs.cloud.google.com/asset-inventory/docs/asset-types)
- [Cloud Asset Inventory roles and permissions](https://docs.cloud.google.com/asset-inventory/docs/roles-permissions)
- [Google Cloud authentication overview](https://docs.cloud.google.com/docs/authentication)
- [Cloud Run services.list](https://docs.cloud.google.com/run/docs/reference/rest/v2/projects.locations.services/list)
- [Cloud Run EnvVar](https://docs.cloud.google.com/run/docs/reference/rest/v2/Container)
- [Resource Manager projects.search](https://cloud.google.com/resource-manager/reference/rest/v3/projects/search)
