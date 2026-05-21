using AzureGraphAI.Core.Azure.Import;
using AzureGraphAI.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddAzureGraphImporter(builder.Configuration);

using var host = builder.Build();
if (args.Any(arg => arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase)))
{
    var dryRunService = host.Services.GetRequiredService<IAzureGraphDryRunService>();
    var dryRun = await dryRunService.RunAsync();

    Console.WriteLine("Dry run complete.");
    Console.WriteLine($"Neo4j: {dryRun.Neo4jServer}");
    foreach (var subscription in dryRun.Subscriptions)
    {
        Console.WriteLine($"Subscription: {subscription.AzureSubscriptionId}");
        Console.WriteLine($"  Name: {subscription.DisplayName}");
        Console.WriteLine($"  State: {subscription.State}");
        Console.WriteLine($"  Tenant: {subscription.TenantId}");
    }

    return;
}

var importer = host.Services.GetRequiredService<IAzureGraphImportService>();
var summary = await importer.ImportAsync();

Console.WriteLine("Import complete.");
foreach (var count in summary.NodeCounts)
    Console.WriteLine($"{count.Key}: {count.Value}");

if (summary.DanglingRelationships.Count > 0)
    Console.WriteLine($"Dangling relationships pruned: {summary.DanglingRelationships.Count}");
