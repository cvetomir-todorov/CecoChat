using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Common.AspNet.Init;
using Common.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CecoChat.Server;

public static class EntryPoint
{
    private const string EnvironmentVariablesPrefix = "CECOCHAT_";

    public static WebApplicationBuilder CreateWebAppBuilder(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables(EnvironmentVariablesPrefix);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.UseSerilog(dispose: true);

        return builder;
    }

    public static async Task RunWebApp(WebApplication app, Type loggerContext)
    {
        Assembly entryAssembly = GetEntryAssembly();
        string environment = GetEnvironment();

        SetupSerilog(environment, entryAssembly);
        ILogger logger = Log.ForContext(loggerContext);

        try
        {
            logger.Information("Starting in {Environment} environment...", environment);

            bool initialized = await app.Services.Init();
            if (!initialized)
            {
                logger.Fatal("Failed to initialize");
                return;
            }

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Unexpected failure");
        }
        finally
        {
            logger.Information("Ended");
            await Log.CloseAndFlushAsync();
        }
    }

    private static Assembly GetEntryAssembly()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
        {
            throw new InvalidOperationException("Entry assembly is null.");
        }

        return entryAssembly;
    }

    private static string GetEnvironment()
    {
        const string aspnetEnvVarName = "ASPNETCORE_ENVIRONMENT";
        string? environment = Environment.GetEnvironmentVariable(aspnetEnvVarName);
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new InvalidOperationException($"Environment variable '{aspnetEnvVarName}' is not set or is whitespace.");
        }

        return environment;
    }

    private static void SetupSerilog(string environment, Assembly entryAssembly)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables(EnvironmentVariablesPrefix)
            .Build();

        OtlpLoggingOptions otlpOptions = new();
        config.GetSection("Telemetry:Logging:Export").Bind(otlpOptions);

        SerilogConfig.Setup(entryAssembly, environment, otlpOptions);
    }
}
