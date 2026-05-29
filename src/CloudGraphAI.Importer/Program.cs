using CloudGraphAI.Azure.Configuration;
using CloudGraphAI.Azure.Import;
using CloudGraphAI.Azure.Extensions;
using CloudGraphAI.Azure.Infrastructure;
using CloudGraphAI.GoogleCloud.Extensions;
using CloudGraphAI.GoogleCloud.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Neo4jLiteRepo.Setup;

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

// Verify Azure CLI identity before proceeding
if (providers.Azure)
{
    var azureOptions = host.Services.GetRequiredService<IOptions<AzureGraphOptions>>().Value;
    if (azureOptions.Authentication.Mode.Equals(AzureGraphAuthenticationModes.AzureCli, StringComparison.OrdinalIgnoreCase))
    {
        var config = host.Services.GetRequiredService<IConfiguration>();
        var tenantId = config["AzureGraph:TenantId"];
        var account = await AzureCliIdentity.GetCurrentAccountAsync(tenantId);
        if (account.User is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to retrieve Azure CLI user. Ensure you are logged in with 'az login'.");
            Console.ResetColor();
            Environment.ExitCode = 1;
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Logged in as: {account.User}");
        Console.WriteLine($"Active subscription: {account.SubscriptionName ?? "unknown"} ({account.SubscriptionId ?? "unknown"})");
        Console.WriteLine($"Target subscriptions: {string.Join(", ", azureOptions.IncludedSubscriptions)}");
        Console.WriteLine($"Neo4j database: {config["Neo4jSettings:Database"]} ({config["Neo4jSettings:ConnectionUri"]})");
        Console.ResetColor();

        if (account.SubscriptionId is not null &&
            !azureOptions.IncludedSubscriptions.Contains(account.SubscriptionId, StringComparer.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: Active subscription ({account.SubscriptionId}) is not in the target subscriptions list.");
            Console.ResetColor();
        }

        Console.Write("Continue with this Azure identity? [Y/n] ");
        var response = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(response) && !response.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Aborted by user.");
            Environment.ExitCode = 0;
            return;
        }
    }
}

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

try
{
    // Ensure the configured Neo4j database exists before importing
    var dbInitializer = host.Services.GetRequiredService<INeo4jDatabaseInitializer>();
    var dbReady = await dbInitializer.EnsureDatabaseExistsAsync();
    if (!dbReady)
    {
        Environment.ExitCode = 1;
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
}
catch (ServiceUnavailableException ex)
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CloudGraphAI.Importer");
    logger.LogDebug(ex, "Neo4j connectivity failure details");

    var config = host.Services.GetRequiredService<IConfiguration>();
    var connectionUri = config["Neo4jSettings:ConnectionUri"] ?? "(not configured)";

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine();
    Console.WriteLine("  Neo4j is unavailable.");
    Console.WriteLine();
    Console.WriteLine($"  Could not connect to: {connectionUri}");
    Console.WriteLine();
    Console.WriteLine("  Please check that:");
    Console.WriteLine("    - Neo4j is running and accepting connections");
    Console.WriteLine("    - The host and port in Neo4jSettings:ConnectionUri are correct");
    Console.WriteLine("    - No firewall is blocking the connection");
    Console.WriteLine("    - Encryption settings are compatible (Neo4j 4.0+ changed defaults)");
    Console.WriteLine();
    Console.ResetColor();

    Environment.ExitCode = 1;
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
