using AzureGraphAI.AI.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Neo4j.Driver;
using Neo4jLiteRepo;
using Neo4jLiteRepo.Setup;

namespace AzureGraphAI.AI.Extensions;

public static class KernelBuilderExtensions
{
    public static IKernelBuilder AddAzureGraphNeo4jTools(this IKernelBuilder builder, IConfiguration configuration)
    {
        builder.Services.AddSingleton(configuration);
        builder.Services.AddSingleton<IDriver>(_ =>
        {
            var settings = new Neo4jSettings { User = "", Password = "", Database = "" };
            configuration.GetSection("Neo4jSettings").Bind(settings);
            var connection = settings.ConnectionUri?.ToString() ?? configuration["Neo4jSettings:Connection"];
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Neo4jSettings:ConnectionUri is required.");

            return GraphDatabase.Driver(connection, AuthTokens.Basic(settings.User, settings.Password));
        });
        builder.Services.AddSingleton<IDataSourceService, DataSourceService>();
        builder.Services.AddSingleton<INeo4jGenericRepo, Neo4jGenericRepo>();
        builder.Services.AddSingleton<IGraphChatRunner, GraphChatRunner>();
        builder.Plugins.AddFromType<Neo4jQueryPlugin>("neo4j");
        return builder;
    }
}
