using System.Net;
using Agile.API.Clients;
using CloudGraphAI.Azure.Api;
using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Environments;
using CloudGraphAI.Azure.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Neo4jLiteRepo;
using Neo4jLiteRepo.Setup;

namespace CloudGraphAI.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureGraphImporter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.Configure<AzureGraphOptions>(configuration.GetSection("AzureGraph"));
        services.Configure<DisplayNameOptions>(configuration.GetSection("AzureGraph:DisplayName"));

        services.AddHttpClient(ApiBase.DefaultHttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddPolicyHandler((_, _) => RetryPolicies.GetRetryPolicies());

        services.AddSingleton<IMicrosoftLoginApi, MicrosoftLoginApi>();
        services.AddSingleton<IMicrosoftTokenProvider, MicrosoftTokenProvider>();
        services.AddSingleton<IAzureRestApi, AzureRestApi>();
        services.AddSingleton<IResourceEnvironmentResolver, ConfigurationResourceEnvironmentResolver>();
        services.AddSingleton<IDisplayNameFormatter, DisplayNameFormatter>();

        services.AddSingleton<IAzureResourceCollector, SubscriptionCollector>();
        services.AddSingleton<IAzureResourceCollector, ResourceGroupCollector>();
        services.AddSingleton<IAzureResourceCollector, VNetCollector>();
        services.AddSingleton<IAzureResourceCollector, StorageAccountCollector>();
        services.AddSingleton<IAzureResourceCollector, KeyVaultCollector>();
        services.AddSingleton<IAzureResourceCollector, UserAssignedManagedIdentityCollector>();
        services.AddSingleton<IAzureResourceCollector, ServerFarmCollector>();
        services.AddSingleton<IAzureResourceCollector, ContainerRegistryCollector>();
        services.AddSingleton<IAzureResourceCollector, CosmosDbAccountCollector>();
        services.AddSingleton<IAzureResourceCollector, AzureAIFoundryAccountCollector>();
        services.AddSingleton<IAzureResourceCollector, RedisCacheCollector>();
        services.AddSingleton<IAzureResourceCollector, SqlManagedInstanceCollector>();
        services.AddSingleton<IAzureResourceCollector, ContainerAppCollector>();
        services.AddSingleton<IAzureResourceCollector, WebAppCollector>();
        services.AddSingleton<IAzureResourceCollector, ResourceCostCollector>();

        services.AddSingleton<IAzureGraphImportService, AzureGraphImportService>();
        services.AddSingleton<IAzureGraphDryRunService, AzureGraphDryRunService>();
        services.AddNeo4jLiteRepo(configuration);

        return services;
    }

    public static IServiceCollection AddNeo4jLiteRepo(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDriver>(_ =>
        {
            var settings = new Neo4jSettings { User = "", Password = "", Database = "" };
            configuration.GetSection("Neo4jSettings").Bind(settings);
            var connection = settings.ConnectionUri?.ToString() ?? configuration["Neo4jSettings:Connection"];
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Neo4jSettings:ConnectionUri is required.");

            return GraphDatabase.Driver(connection, AuthTokens.Basic(settings.User, settings.Password));
        });

        services.AddSingleton<INeo4jDatabaseInitializer, Neo4jDatabaseInitializer>();
        services.AddSingleton<IDataSourceService, DataSourceService>();
        services.AddSingleton<INeo4jGenericRepo, Neo4jGenericRepo>();
        return services;
    }
}
