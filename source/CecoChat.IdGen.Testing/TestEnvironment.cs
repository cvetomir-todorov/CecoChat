using System.Reflection;
using NUnit.Framework;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace CecoChat.IdGen.Testing;

[SetUpFixture]
public class TestEnvironment
{
    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        InitEnv();
        ConfigureLogging();
    }

    private static void InitEnv()
    {
        const string envName = "ASPNETCORE_ENVIRONMENT";
        string? env = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(env))
        {
            env = "Development";
            Environment.SetEnvironmentVariable(envName, env);
        }

        TestContext.Progress.WriteLine($"{envName}: {env}");
    }

    private static void ConfigureLogging()
    {
        Assembly entryAssembly = Assembly.GetEntryAssembly()!;
        string name = entryAssembly.GetName().Name!;
        string binPath = Path.GetDirectoryName(entryAssembly.Location) ?? Environment.CurrentDirectory;
        // going from /source/project/bin/debug/.netX.Y/ to /source/logs/project.txt
        string filePath = Path.Combine(binPath, "..", "..", "..", "..", "logs", $"{name}.txt");

        LoggerConfiguration loggerConfig = new();

        loggerConfig
            .MinimumLevel.Is(LogEventLevel.Information)
            .MinimumLevel.Override("CecoChat", LogEventLevel.Verbose)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithSpan()
            .Enrich.FromLogContext()
            .Destructure.ToMaximumDepth(8)
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumCollectionCount(32)
            .WriteTo.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}");

        Log.Logger = loggerConfig.CreateLogger();
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        Log.CloseAndFlush();
    }
}
