using System.Reflection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace CecoChat.Server;

public static class SerilogConfig
{
    private const string LogEntryConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}";

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
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", entryAssembly.GetName().Name!)
            .Enrich.WithSpan()
            .Enrich.FromLogContext()
            .Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumCollectionCount(32)
            .WriteTo.Console(
                outputTemplate: LogEntryConsoleOutputTemplate)
            .WriteTo.OpenTelemetry(otel =>
            {
                otel.Endpoint = "http://localhost:4317";
                otel.Protocol = OtlpProtocol.Grpc;
                otel.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;

                otel.BatchingOptions.EagerlyEmitFirstEvent = true;
                otel.BatchingOptions.Period = TimeSpan.FromSeconds(1);
                otel.BatchingOptions.BatchSizeLimit = 1000;
                otel.BatchingOptions.QueueLimit = 100_000;
            });
    }

    private static LoggerConfiguration ApplyDevelopmentConfiguration(Assembly entryAssembly, LoggerConfiguration configuration)
    {
        string name = entryAssembly.GetName().Name!;
        string binPath = Path.GetDirectoryName(entryAssembly.Location) ?? Environment.CurrentDirectory;
        // going from /source/project/bin/debug/.net7.0/ to /source/logs/project.txt
        string filePath = Path.Combine(binPath, "..", "..", "..", "..", "logs", $"{name}.txt");

        return configuration
            .MinimumLevel.Override("CecoChat", LogEventLevel.Verbose)
            .WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: LogEntryConsoleOutputTemplate);
    }

    private static LoggerConfiguration ApplyProductionConfiguration(LoggerConfiguration configuration)
    {
        return configuration;
    }
}
