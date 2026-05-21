using AzureGraphAI.AI;
using AzureGraphAI.AI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

var apiKey = builder.Configuration["AI:OpenAI:ApiKey"]
             ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("Set AI:OpenAI:ApiKey or OPENAI_API_KEY.");

var modelId = builder.Configuration["AI:OpenAI:ModelId"];
if (string.IsNullOrWhiteSpace(modelId))
    throw new InvalidOperationException("Set AI:OpenAI:ModelId.");

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddLogging(logging => logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));
kernelBuilder.Services.AddSingleton(builder.Configuration);
kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
kernelBuilder.AddAzureGraphNeo4jTools(builder.Configuration);

var kernel = kernelBuilder.Build();
var runner = kernel.GetRequiredService<IGraphChatRunner>();
await runner.RunAsync(kernel);
