using System.Net.Http.Headers;
using Agile.API.Clients;
using Agile.API.Clients.CallHandling;
using AzureGraphAI.Core.Azure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureGraphAI.Core.Azure.Api;

public interface IAzureRestApi
{
    Task<CallResult<Subscription>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<ResourceGroup>>> GetResourceGroupsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<StorageAccount>>> GetStorageAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<KeyVault>>> GetKeyVaultsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<UserAssignedManagedIdentity>>> GetUserAssignedManagedIdentitiesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<WebApp>>> GetWebAppsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<ServerFarm>>> GetServerFarmsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<VNet>>> GetVNetsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<ContainerApp>>> GetContainerAppsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<ContainerRegistry>>> GetContainerRegistriesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<CosmosDbAccount>>> GetCosmosDbAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<AzureAIFoundryAccount>>> GetAzureAIFoundryAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<RedisCache>>> GetRedisCachesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<SqlManagedInstance>>> GetSqlManagedInstancesAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<T>>> GetNextPageAsync<T>(string nextLink, CancellationToken cancellationToken = default)
        where T : AzureGraphNode;
    Task<CallResult<WebAppConfig>> GetWebAppSiteConfigAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default);
    Task<CallResult<AzureResourceListResult<WebJob>>> GetWebAppContinuousWebJobsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default);
    Task<CallResult<AppSettings>> GetWebAppSettingsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default);
    Task<CallResult<ConnectionStrings>> GetWebAppConnectionStringsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default);
}

public sealed class AzureRestApi : ApiBase, IAzureRestApi
{
    private readonly IMicrosoftTokenProvider _tokenProvider;
    private readonly ILogger<AzureRestApi> _logger;
    private readonly ApiMethod<Subscription> _getSubscription;
    private readonly ApiMethod<AzureResourceListResult<ResourceGroup>> _getResourceGroups;
    private readonly ApiMethod<AzureResourceListResult<StorageAccount>> _getStorageAccounts;
    private readonly ApiMethod<AzureResourceListResult<KeyVault>> _getKeyVaults;
    private readonly ApiMethod<AzureResourceListResult<UserAssignedManagedIdentity>> _getUserAssignedManagedIdentities;
    private readonly ApiMethod<AzureResourceListResult<WebApp>> _getWebApps;
    private readonly ApiMethod<AzureResourceListResult<ServerFarm>> _getServerFarms;
    private readonly ApiMethod<AzureResourceListResult<VNet>> _getVNets;
    private readonly ApiMethod<AzureResourceListResult<ContainerApp>> _getContainerApps;
    private readonly ApiMethod<AzureResourceListResult<ContainerRegistry>> _getContainerRegistries;
    private readonly ApiMethod<AzureResourceListResult<CosmosDbAccount>> _getCosmosDbAccounts;
    private readonly ApiMethod<AzureResourceListResult<AzureAIFoundryAccount>> _getAzureAIFoundryAccounts;
    private readonly ApiMethod<AzureResourceListResult<RedisCache>> _getRedisCaches;
    private readonly ApiMethod<AzureResourceListResult<SqlManagedInstance>> _getSqlManagedInstances;
    private readonly ApiMethod<WebAppConfig> _getWebAppConfig;
    private readonly ApiMethod<AzureResourceListResult<WebJob>> _getWebJobs;
    private readonly ApiMethod<AppSettings> _postAppSettings;
    private readonly ApiMethod<ConnectionStrings> _postConnectionStrings;

    public AzureRestApi(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IMicrosoftTokenProvider tokenProvider,
        ILogger<AzureRestApi> logger)
        : base(configuration, httpClientFactory)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
        _getSubscription = PrivateGet<Subscription>(MethodPriority.Normal);
        _getResourceGroups = PrivateGet<AzureResourceListResult<ResourceGroup>>(MethodPriority.Normal);
        _getStorageAccounts = PrivateGet<AzureResourceListResult<StorageAccount>>(MethodPriority.Normal);
        _getKeyVaults = PrivateGet<AzureResourceListResult<KeyVault>>(MethodPriority.Normal);
        _getUserAssignedManagedIdentities = PrivateGet<AzureResourceListResult<UserAssignedManagedIdentity>>(MethodPriority.Normal);
        _getWebApps = PrivateGet<AzureResourceListResult<WebApp>>(MethodPriority.Normal);
        _getServerFarms = PrivateGet<AzureResourceListResult<ServerFarm>>(MethodPriority.Normal);
        _getVNets = PrivateGet<AzureResourceListResult<VNet>>(MethodPriority.Normal);
        _getContainerApps = PrivateGet<AzureResourceListResult<ContainerApp>>(MethodPriority.Normal);
        _getContainerRegistries = PrivateGet<AzureResourceListResult<ContainerRegistry>>(MethodPriority.Normal);
        _getCosmosDbAccounts = PrivateGet<AzureResourceListResult<CosmosDbAccount>>(MethodPriority.Normal);
        _getAzureAIFoundryAccounts = PrivateGet<AzureResourceListResult<AzureAIFoundryAccount>>(MethodPriority.Normal);
        _getRedisCaches = PrivateGet<AzureResourceListResult<RedisCache>>(MethodPriority.Normal);
        _getSqlManagedInstances = PrivateGet<AzureResourceListResult<SqlManagedInstance>>(MethodPriority.Normal);
        _getWebAppConfig = PrivateGet<WebAppConfig>(MethodPriority.Normal);
        _getWebJobs = PrivateGet<AzureResourceListResult<WebJob>>(MethodPriority.Normal);
        _postAppSettings = PrivatePost<AppSettings>(MethodPriority.Normal);
        _postConnectionStrings = PrivatePost<ConnectionStrings>(MethodPriority.Normal);
    }

    protected override string BaseUrl => "https://management.azure.com/subscriptions";

    public override string ApiId => nameof(AzureRestApi);

    public Task<CallResult<Subscription>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getSubscription.Call(subscriptionId, "", "api-version=2016-06-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<ResourceGroup>>> GetResourceGroupsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getResourceGroups.Call($"{subscriptionId}/resourceGroups", "", "api-version=2024-11-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<StorageAccount>>> GetStorageAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getStorageAccounts.Call($"{subscriptionId}/providers/Microsoft.Storage/storageAccounts", "", "api-version=2024-01-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<KeyVault>>> GetKeyVaultsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getKeyVaults.Call($"{subscriptionId}/providers/Microsoft.KeyVault/vaults", "", "api-version=2023-07-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<UserAssignedManagedIdentity>>> GetUserAssignedManagedIdentitiesAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getUserAssignedManagedIdentities.Call($"{subscriptionId}/providers/Microsoft.ManagedIdentity/userAssignedIdentities", "", "api-version=2024-11-30", cancellationToken);

    public Task<CallResult<AzureResourceListResult<WebApp>>> GetWebAppsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getWebApps.Call($"{subscriptionId}/providers/Microsoft.Web/sites", "", "api-version=2024-04-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<ServerFarm>>> GetServerFarmsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getServerFarms.Call($"{subscriptionId}/providers/Microsoft.Web/serverfarms", "", "api-version=2022-03-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<VNet>>> GetVNetsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getVNets.Call($"{subscriptionId}/providers/Microsoft.Network/virtualNetworks", "", "api-version=2024-05-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<ContainerApp>>> GetContainerAppsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getContainerApps.Call($"{subscriptionId}/providers/Microsoft.App/containerApps", "", "api-version=2023-05-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<ContainerRegistry>>> GetContainerRegistriesAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getContainerRegistries.Call($"{subscriptionId}/providers/Microsoft.ContainerRegistry/registries", "", "api-version=2024-11-01-preview", cancellationToken);

    public Task<CallResult<AzureResourceListResult<CosmosDbAccount>>> GetCosmosDbAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getCosmosDbAccounts.Call($"{subscriptionId}/providers/Microsoft.DocumentDB/databaseAccounts", "", "api-version=2025-10-15", cancellationToken);

    public Task<CallResult<AzureResourceListResult<AzureAIFoundryAccount>>> GetAzureAIFoundryAccountsAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getAzureAIFoundryAccounts.Call($"{subscriptionId}/providers/Microsoft.CognitiveServices/accounts", "", "api-version=2025-06-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<RedisCache>>> GetRedisCachesAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getRedisCaches.Call($"{subscriptionId}/providers/Microsoft.Cache/redis", "", "api-version=2023-08-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<SqlManagedInstance>>> GetSqlManagedInstancesAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => _getSqlManagedInstances.Call($"{subscriptionId}/providers/Microsoft.Sql/managedInstances", "", "api-version=2021-11-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<T>>> GetNextPageAsync<T>(string nextLink, CancellationToken cancellationToken = default)
        where T : AzureGraphNode
        => PrivateGet<AzureResourceListResult<T>>(MethodPriority.Normal).Call(nextLink, "", cancellationToken: cancellationToken);

    public Task<CallResult<WebAppConfig>> GetWebAppSiteConfigAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default)
        => _getWebAppConfig.Call($"{subscriptionId}/resourceGroups/{webApp.ResourceGroupName}/providers/Microsoft.Web/sites/{webApp.Name}/config", "", "api-version=2022-03-01", cancellationToken);

    public Task<CallResult<AzureResourceListResult<WebJob>>> GetWebAppContinuousWebJobsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default)
        => _getWebJobs.Call($"{subscriptionId}/resourceGroups/{webApp.ResourceGroupName}/providers/Microsoft.Web/sites/{webApp.Name}/continuouswebjobs", "", "api-version=2022-03-01", cancellationToken);

    public Task<CallResult<AppSettings>> GetWebAppSettingsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default)
        => _postAppSettings.Call($"{subscriptionId}/resourceGroups/{webApp.ResourceGroupName}/providers/Microsoft.Web/sites/{webApp.Name}/config/appsettings/list", "", "api-version=2022-03-01", cancellationToken);

    public Task<CallResult<ConnectionStrings>> GetWebAppConnectionStringsAsync(string subscriptionId, WebApp webApp, CancellationToken cancellationToken = default)
        => _postConnectionStrings.Call($"{subscriptionId}/resourceGroups/{webApp.ResourceGroupName}/providers/Microsoft.Web/sites/{webApp.Name}/config/connectionstrings/list", "", "api-version=2022-03-01", cancellationToken);

    protected override async Task SetPrivateRequestProperties(HttpRequestMessage request, string method, object? rawPayload = null, string propsWithNonce = "")
    {
        var token = await _tokenProvider.GetManagementTokenAsync().ConfigureAwait(false);
        SetAuthorizationHeader(new AuthenticationHeaderValue(token.TokenType, token.AccessToken));
    }

    protected override void NotifyError<T>(CallResult<T> result)
        => _logger.LogError("{ApiId} {StatusCode} {Uri} {Error} {RawText}", ApiId, result.StatusCode, result.AbsoluteUri, result.Exception?.Message, result.RawText);
}
