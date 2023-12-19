using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using CecoChat.AspNet.Init;
using CecoChat.Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CecoChat.Server;

public static class EntryPoint
{
    private const string EnvironmentVariablesPrefix = "CECOCHAT_";

    public static IHostBuilder CreateDefaultHostBuilder(
        string[] args,
        Type startupContext,
        bool useAutofac = true,
        bool useSerilog = true)
    {
        IHostBuilder hostBuilder = Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup(startupContext);
            })
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddEnvironmentVariables(EnvironmentVariablesPrefix);
            });

        if (useAutofac)
        {
            hostBuilder = hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        }
        if (useSerilog)
        {
            hostBuilder = hostBuilder.UseSerilog();
        }

        return hostBuilder;
    }

    public static async Task CreateAndRunHost(IHostBuilder hostBuilder, Type loggerContext)
    {
        Assembly entryAssembly = GetEntryAssembly();
        string environment = GetEnvironment();

        SetupSerilog(environment, entryAssembly);
        ILogger logger = Log.ForContext(loggerContext);

        try
        {
            logger.Information("Starting in {Environment} environment...", environment);
            IHost host = hostBuilder.Build();

            bool initialized = await host.Init();
            if (!initialized)
            {
                logger.Fatal("Failed to initialize");
                return;
            }

            await host.RunAsync();
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

        SerilogOtlpOptions otlpOptions = new();
        config.GetSection("Telemetry:Logging:Export").Bind(otlpOptions);

        SerilogConfig.Setup(entryAssembly, environment, otlpOptions);
    }
}
