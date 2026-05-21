using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Extensions;
using CloudGraphAI.GoogleCloud.Extensions;
using CloudGraphAI.GoogleCloud.Import;
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

var providers = EnabledGraphProviders.Load(builder.Configuration);
if (!providers.Azure && !providers.GoogleCloud)
    throw new InvalidOperationException("No graph providers enabled. Enable CloudGraph:Providers:Azure:Enabled or CloudGraph:Providers:GoogleCloud:Enabled.");

if (providers.Azure)
    builder.Services.AddAzureGraphImporter(builder.Configuration);

if (providers.GoogleCloud)
    builder.Services.AddGoogleCloudGraphImporter(builder.Configuration);

using var host = builder.Build();
if (args.Any(arg => arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase)))
{
    var succeeded = true;
    Console.WriteLine("Dry run complete.");

    if (providers.Azure)
    {
        var dryRunService = host.Services.GetRequiredService<IAzureGraphDryRunService>();
        var dryRun = await dryRunService.RunAsync();
        succeeded &= dryRun.Succeeded;
        PrintAzureDryRun(dryRun);
    }

    if (providers.GoogleCloud)
    {
        var dryRunService = host.Services.GetRequiredService<IGoogleCloudGraphDryRunService>();
        var dryRun = await dryRunService.RunAsync();
        succeeded &= dryRun.Succeeded;
        PrintGoogleCloudDryRun(dryRun);
    }

    Environment.ExitCode = succeeded ? 0 : 1;
    return;
}

if (providers.Azure)
{
    var importer = host.Services.GetRequiredService<IAzureGraphImportService>();
    var summary = await importer.ImportAsync();
    PrintImportSummary("Azure", summary.NodeCounts, summary.DanglingRelationships.Count);
}

if (providers.GoogleCloud)
{
    var importer = host.Services.GetRequiredService<IGoogleCloudGraphImportService>();
    var summary = await importer.ImportAsync();
    PrintImportSummary("Google Cloud", summary.NodeCounts, summary.DanglingRelationships.Count);
}

static void PrintAzureDryRun(AzureGraphDryRunSummary dryRun)
{
    Console.WriteLine("Azure:");
    PrintNeo4jSettings(
        dryRun.Neo4jConnectionUri,
        dryRun.Neo4jUser,
        dryRun.Neo4jDatabase,
        dryRun.Neo4jPasswordConfigured,
        dryRun.Neo4jPasswordLength,
        dryRun.Neo4jSucceeded,
        dryRun.Neo4jServer,
        dryRun.Neo4jError);

    foreach (var result in dryRun.SubscriptionResults)
    {
        if (!result.Succeeded || result.Subscription is null)
        {
            Console.WriteLine($"  Subscription: FAILED ({result.SubscriptionId})");
            Console.WriteLine($"    Error: {result.Error}");
            continue;
        }

        var subscription = result.Subscription;
        Console.WriteLine($"  Subscription: OK ({subscription.AzureSubscriptionId})");
        Console.WriteLine($"    Name: {subscription.DisplayName}");
        Console.WriteLine($"    State: {subscription.State}");
        Console.WriteLine($"    Tenant: {subscription.TenantId}");
    }
}

static void PrintGoogleCloudDryRun(GoogleCloudGraphDryRunSummary dryRun)
{
    Console.WriteLine("Google Cloud:");
    PrintNeo4jSettings(
        dryRun.Neo4jConnectionUri,
        dryRun.Neo4jUser,
        dryRun.Neo4jDatabase,
        dryRun.Neo4jPasswordConfigured,
        dryRun.Neo4jPasswordLength,
        dryRun.Neo4jSucceeded,
        dryRun.Neo4jServer,
        dryRun.Neo4jError);

    Console.WriteLine($"  Auth mode: {dryRun.AuthenticationMode}");
    Console.WriteLine($"  API key configured: {dryRun.ApiKeyConfigured}");

    foreach (var result in dryRun.ScopeResults)
    {
        if (!result.Succeeded)
        {
            Console.WriteLine($"  Scope: FAILED ({result.Scope})");
            Console.WriteLine($"    Error: {result.Error}");
            continue;
        }

        Console.WriteLine($"  Scope: OK ({result.Scope})");
        Console.WriteLine($"    First-page asset count: {result.AssetCount}");
    }
}

static void PrintNeo4jSettings(
    string? connectionUri,
    string? user,
    string? database,
    bool passwordConfigured,
    int passwordLength,
    bool succeeded,
    string? server,
    string? error)
{
    Console.WriteLine("  Neo4j settings:");
    Console.WriteLine($"    Uri: {connectionUri}");
    Console.WriteLine($"    User: {user}");
    Console.WriteLine($"    Database: {database}");
    Console.WriteLine($"    Password configured: {passwordConfigured} (length {passwordLength})");
    Console.WriteLine(succeeded
        ? $"    Neo4j: OK ({server})"
        : $"    Neo4j: FAILED ({error})");
}

static void PrintImportSummary(string providerName, IReadOnlyDictionary<string, int> nodeCounts, int danglingRelationshipCount)
{
    Console.WriteLine($"{providerName} import complete.");
    foreach (var count in nodeCounts)
        Console.WriteLine($"{providerName} {count.Key}: {count.Value}");

    if (danglingRelationshipCount > 0)
        Console.WriteLine($"{providerName} dangling relationships pruned: {danglingRelationshipCount}");
}

internal sealed record EnabledGraphProviders(bool Azure, bool GoogleCloud)
{
    public static EnabledGraphProviders Load(IConfiguration configuration)
        => new(
            GetProviderEnabled(configuration, "Azure", defaultValue: true),
            GetProviderEnabled(configuration, "GoogleCloud", defaultValue: false));

    private static bool GetProviderEnabled(IConfiguration configuration, string provider, bool defaultValue)
    {
        var raw = configuration[$"CloudGraph:Providers:{provider}:Enabled"];
        return bool.TryParse(raw, out var enabled) ? enabled : defaultValue;
    }
}
