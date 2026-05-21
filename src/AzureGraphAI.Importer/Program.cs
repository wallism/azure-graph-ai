using AzureGraphAI.Core.Azure.Import;
using AzureGraphAI.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

builder.Services.AddAzureGraphImporter(builder.Configuration);

using var host = builder.Build();
if (args.Any(arg => arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase)))
{
    var dryRunService = host.Services.GetRequiredService<IAzureGraphDryRunService>();
    var dryRun = await dryRunService.RunAsync();

    Console.WriteLine("Dry run complete.");
    Console.WriteLine("Neo4j settings:");
    Console.WriteLine($"  Uri: {dryRun.Neo4jConnectionUri}");
    Console.WriteLine($"  User: {dryRun.Neo4jUser}");
    Console.WriteLine($"  Database: {dryRun.Neo4jDatabase}");
    Console.WriteLine($"  Password configured: {dryRun.Neo4jPasswordConfigured} (length {dryRun.Neo4jPasswordLength})");
    Console.WriteLine(dryRun.Neo4jSucceeded
        ? $"Neo4j: OK ({dryRun.Neo4jServer})"
        : $"Neo4j: FAILED ({dryRun.Neo4jError})");

    foreach (var result in dryRun.SubscriptionResults)
    {
        if (!result.Succeeded || result.Subscription is null)
        {
            Console.WriteLine($"Subscription: FAILED ({result.SubscriptionId})");
            Console.WriteLine($"  Error: {result.Error}");
            continue;
        }

        var subscription = result.Subscription;
        Console.WriteLine($"Subscription: OK ({subscription.AzureSubscriptionId})");
        Console.WriteLine($"  Name: {subscription.DisplayName}");
        Console.WriteLine($"  State: {subscription.State}");
        Console.WriteLine($"  Tenant: {subscription.TenantId}");
    }

    Environment.ExitCode = dryRun.Succeeded ? 0 : 1;
    return;
}

var importer = host.Services.GetRequiredService<IAzureGraphImportService>();
var summary = await importer.ImportAsync();

Console.WriteLine("Import complete.");
foreach (var count in summary.NodeCounts)
    Console.WriteLine($"{count.Key}: {count.Value}");

if (summary.DanglingRelationships.Count > 0)
    Console.WriteLine($"Dangling relationships pruned: {summary.DanglingRelationships.Count}");
