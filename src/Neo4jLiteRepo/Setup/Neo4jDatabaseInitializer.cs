using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Neo4jLiteRepo.Setup;

public interface INeo4jDatabaseInitializer
{
    /// <summary>
    /// Ensures the configured database exists and is online.
    /// On Enterprise edition, creates it automatically if missing.
    /// On Community edition, logs guidance for the user.
    /// </summary>
    Task<bool> EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default);
}

public class Neo4jDatabaseInitializer(
    ILogger<Neo4jDatabaseInitializer> logger,
    IDriver driver,
    IConfiguration configuration) : INeo4jDatabaseInitializer
{
    private static readonly TimeSpan DatabaseOnlineTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    public async Task<bool> EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        var databaseName = configuration["Neo4jSettings:Database"];

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            logger.LogDebug("No database name configured; using Neo4j default database");
            return true;
        }

        logger.LogInformation("Checking Neo4j database '{Database}' exists...", databaseName);

        var (edition, version) = await GetServerEditionAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Neo4j server: {Edition} edition, version {Version}", edition, version);

        // Check if the database already exists
        var (exists, isOnline) = await CheckDatabaseStatusAsync(databaseName, cancellationToken).ConfigureAwait(false);

        if (exists && isOnline)
        {
            logger.LogInformation("Database '{Database}' is online and ready", databaseName);
            return true;
        }

        if (exists && !isOnline)
        {
            logger.LogInformation("Database '{Database}' exists but is not yet online. Waiting...", databaseName);
            return await WaitForDatabaseOnlineAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }

        // Database does not exist
        var isCommunity = edition.Contains("community", StringComparison.OrdinalIgnoreCase);

        if (isCommunity)
        {
            logger.LogError(
                "Neo4j Community Edition detected. Community Edition only supports the default 'neo4j' database. " +
                "You have configured Database=\"{Database}\" which does not exist.\n\n" +
                "You have two options:\n" +
                "  1. Change Neo4jSettings:Database to \"neo4j\" (or leave it empty) in your appsettings.json\n" +
                "  2. Upgrade to Neo4j Enterprise Edition and the database will be created automatically\n\n" +
                "If you are using Enterprise Edition, you can create the database manually with:\n" +
                "  CREATE DATABASE `{Database}` IF NOT EXISTS",
                databaseName, databaseName);
            return false;
        }

        // Enterprise edition - create the database
        logger.LogInformation("Creating database '{Database}'...", databaseName);
        await CreateDatabaseAsync(databaseName, cancellationToken).ConfigureAwait(false);

        return await WaitForDatabaseOnlineAsync(databaseName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(string Edition, string Version)> GetServerEditionAsync(CancellationToken cancellationToken)
    {
        await using var session = driver.AsyncSession(o => o.WithDatabase("system"));
        try
        {
            var result = await session.RunAsync("CALL dbms.components() YIELD name, versions, edition").ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);

            var edition = record["edition"].As<string>() ?? "unknown";
            var versions = record["versions"].As<IList<object>>();
            var version = versions?.FirstOrDefault()?.ToString() ?? "unknown";

            return (edition, version);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to determine Neo4j edition via dbms.components(). Attempting SHOW DATABASE fallback");
            // Fallback: if we can't determine edition, try to create anyway and let it fail gracefully
            return ("enterprise", "unknown");
        }
    }

    private async Task<(bool Exists, bool IsOnline)> CheckDatabaseStatusAsync(string databaseName, CancellationToken cancellationToken)
    {
        await using var session = driver.AsyncSession(o => o.WithDatabase("system"));
        try
        {
            var result = await session.RunAsync(
                "SHOW DATABASE $name YIELD currentStatus",
                new { name = databaseName }).ConfigureAwait(false);

            var records = await result.ToListAsync().ConfigureAwait(false);

            if (records.Count == 0)
                return (false, false);

            var status = records[0]["currentStatus"].As<string>();
            return (true, string.Equals(status, "online", StringComparison.OrdinalIgnoreCase));
        }
        catch (ClientException ex) when (ex.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase) ||
                                          ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return (false, false);
        }
        catch (DatabaseException ex) when (ex.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase) ||
                                            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return (false, false);
        }
    }

    private async Task CreateDatabaseAsync(string databaseName, CancellationToken cancellationToken)
    {
        await using var session = driver.AsyncSession(o => o.WithDatabase("system"));
        try
        {
            // Database names with special characters need backtick quoting.
            // Parameters cannot be used for database names in DDL statements,
            // so we sanitize and use string interpolation with backtick quoting.
            var safeName = SanitizeDatabaseName(databaseName);
            await session.RunAsync($"CREATE DATABASE `{safeName}` IF NOT EXISTS").ConfigureAwait(false);
            logger.LogInformation("CREATE DATABASE command issued for '{Database}'", databaseName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create database '{Database}'. " +
                "Ensure the Neo4j user has admin privileges. " +
                "You can create it manually by running the following Cypher against the system database:\n" +
                "  :use system\n" +
                "  CREATE DATABASE `{Database}` IF NOT EXISTS",
                databaseName, databaseName);
            throw;
        }
    }

    private async Task<bool> WaitForDatabaseOnlineAsync(string databaseName, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + DatabaseOnlineTimeout;

        logger.LogInformation("Waiting for database '{Database}' to come online (timeout: {Timeout}s)...",
            databaseName, DatabaseOnlineTimeout.TotalSeconds);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (exists, isOnline) = await CheckDatabaseStatusAsync(databaseName, cancellationToken).ConfigureAwait(false);

            if (exists && isOnline)
            {
                logger.LogInformation("Database '{Database}' is now online", databaseName);
                return true;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        logger.LogError(
            "Timed out waiting for database '{Database}' to come online after {Timeout}s. " +
            "Check Neo4j server logs for errors. You can check status with:\n" +
            "  :use system\n" +
            "  SHOW DATABASE `{Database}` YIELD name, currentStatus",
            databaseName, DatabaseOnlineTimeout.TotalSeconds, databaseName);
        return false;
    }

    /// <summary>
    /// Sanitizes a database name to prevent injection in Cypher DDL.
    /// Removes backticks and other control characters.
    /// </summary>
    private static string SanitizeDatabaseName(string name)
    {
        // Neo4j database names: alphanumeric, dots, dashes, underscores
        // Remove anything that could break out of backtick quoting
        return name.Replace("`", "").Replace("\\", "").Replace("\n", "").Replace("\r", "");
    }
}
