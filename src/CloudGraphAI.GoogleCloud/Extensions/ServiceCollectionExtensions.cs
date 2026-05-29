using CloudGraphAI.GoogleCloud.Api;
using CloudGraphAI.GoogleCloud.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Neo4jLiteRepo;
using Neo4jLiteRepo.Setup;

namespace CloudGraphAI.GoogleCloud.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleCloudGraphImporter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        services.AddHttpClient(GoogleCloudRestApi.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddSingleton<IGoogleCloudTokenProvider, GoogleCloudTokenProvider>();
        services.AddSingleton<IGoogleCloudRestApi, GoogleCloudRestApi>();

        services.AddSingleton<IGoogleCloudResourceCollector, GoogleOrganizationCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleFolderCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleProjectCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleNetworkCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleSubnetworkCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleServiceAccountCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleArtifactRepositoryCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleSecretCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleStorageBucketCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleCloudSqlInstanceCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleVertexAiEndpointCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleMemorystoreRedisCollector>();
        services.AddSingleton<IGoogleCloudResourceCollector, GoogleCloudRunServiceCollector>();

        services.AddSingleton<IGoogleCloudGraphImportService, GoogleCloudGraphImportService>();
        services.AddSingleton<IGoogleCloudGraphDryRunService, GoogleCloudGraphDryRunService>();
        services.AddGoogleCloudNeo4jLiteRepo(configuration);

        return services;
    }

    private static IServiceCollection AddGoogleCloudNeo4jLiteRepo(this IServiceCollection services, IConfiguration configuration)
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
