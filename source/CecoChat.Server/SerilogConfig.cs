using System.Reflection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;

namespace CecoChat.Server;

public static class SerilogConfig
{
    public static void Setup(Assembly entryAssembly, string environment)
    {
        LoggerConfiguration configuration = CreateDefaultConfiguration(entryAssembly);

        switch (environment.ToLowerInvariant())
        {
            case "development":
                configuration = ApplyDevelopmentConfiguration(entryAssembly, configuration);
                break;
            case "production":
                configuration = ApplyProductionConfiguration(configuration);
                break;
            default:
                throw new InvalidOperationException($"Logging configuration doesn't support environment '{environment}'.");
        }

        Log.Logger = configuration.CreateLogger();
    }

    private static LoggerConfiguration CreateDefaultConfiguration(Assembly entryAssembly)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", entryAssembly.GetName().Name!)
            .Enrich.WithSpan()
            .Enrich.FromLogContext()
            .Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumCollectionCount(32);
    }

    private static LoggerConfiguration ApplyDevelopmentConfiguration(Assembly entryAssembly, LoggerConfiguration configuration)
    {
        string name = entryAssembly.GetName().Name!;
        string binPath = Path.GetDirectoryName(entryAssembly.Location) ?? Environment.CurrentDirectory;
        // going from /source/project/bin/debug/.net6.0/ to /source/logs/project.txt
        string filePath = Path.Combine(binPath, "..", "..", "..", "..", "logs", $"{name}.txt");

        return configuration
            .MinimumLevel.Override("CecoChat", LogEventLevel.Verbose)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}")
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions
            {
                CustomFormatter = new ElasticsearchJsonFormatter(renderMessageTemplate: false),
                IndexFormat = "cecochat-logs-app"
            })
            .WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}");
    }

    private static LoggerConfiguration ApplyProductionConfiguration(LoggerConfiguration configuration)
    {
        return configuration
            .WriteTo.Console(new ElasticsearchJsonFormatter(renderMessageTemplate: false));
    }
}
