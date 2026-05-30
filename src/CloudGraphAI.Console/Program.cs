using CloudGraphAI.AI;
using CloudGraphAI.AI.Extensions;
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
builder.Logging.AddInteractiveConsoleLogging();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddLogging(logging => logging.AddInteractiveConsoleLogging());
kernelBuilder.Services.AddSingleton(builder.Configuration);
kernelBuilder.AddConfiguredAIModelProviders(builder.Configuration);
kernelBuilder.AddCloudGraphNeo4jTools(builder.Configuration);

var kernel = kernelBuilder.Build();
var runner = kernel.GetRequiredService<IGraphChatRunner>();
await runner.RunAsync(kernel);

static class InteractiveConsoleLogging
{
    public static void AddInteractiveConsoleLogging(this ILoggingBuilder logging)
    {
        logging.AddFilter("Microsoft.SemanticKernel", LogLevel.Warning);
        logging.AddProvider(new InteractiveConsoleLoggerProvider());
    }
}

sealed class InteractiveConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new InteractiveConsoleLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class InteractiveConsoleLogger(string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel is not LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
                return;

            var prefix = logLevel >= LogLevel.Warning
                ? $"{DateTime.Now:HH:mm:ss} {logLevel.ToString().ToLowerInvariant()}: {categoryName}: "
                : $"{DateTime.Now:HH:mm:ss} ";

            Console.WriteLine($"{prefix}{message}");
            if (exception is not null)
                Console.WriteLine(exception);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
