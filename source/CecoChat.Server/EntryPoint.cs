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

    public static async Task<int> Run(string[] args, Type loggerContext, Action<WebApplicationBuilder> configureBuilder, Action<WebApplication> configurePipeline)
    {
        Assembly entryAssembly = GetEntryAssembly();
        string environment = GetEnvironment();

        SetupSerilog(environment, entryAssembly);
        ILogger logger = Log.ForContext(loggerContext);
        WebApplication? app = null;

        try
        {
            logger.Information("Starting in {Environment} environment...", environment);

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables(EnvironmentVariablesPrefix);
            // command line args over env vars
            builder.Configuration.AddCommandLine(args);
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.UseSerilog(dispose: false);

            configureBuilder(builder);

            app = builder.Build();
            configurePipeline(app);

            bool initialized = await app.Services.Init();
            if (!initialized)
            {
                logger.Fatal("Failed to initialize");
                return 1;
            }

            await app.RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Unexpected failure");
            return 2;
        }
        finally
        {
            try
            {
                if (app != null)
                {
                    await app.DisposeAsync();
                }
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Failure during disposal");
            }

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
