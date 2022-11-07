using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CecoChat.Server;

public static class EntryPoint
{
    public static IHostBuilder CreateDefaultHostBuilder(
        string[] args,
        Type startupContext,
        bool useAutofac = true,
        bool useSerilog = true,
        string environmentVariablesPrefix = "CECOCHAT_")
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        if (useAutofac)
        {
            hostBuilder = hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        }
        if (startupContext != null)
        {
            hostBuilder = hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup(startupContext);
            });
        }
        if (useSerilog)
        {
            hostBuilder = hostBuilder.UseSerilog();
        }
        if (!string.IsNullOrWhiteSpace(environmentVariablesPrefix))
        {
            hostBuilder = hostBuilder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddEnvironmentVariables(environmentVariablesPrefix);
            });
        }

        return hostBuilder;
    }

    public static void CreateAndRunHost(IHostBuilder hostBuilder, Type loggerContext)
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
        {
            throw new InvalidOperationException("Entry assembly is null.");
        }

        const string aspnetEnvVarName = "ASPNETCORE_ENVIRONMENT";
        string? environment = Environment.GetEnvironmentVariable(aspnetEnvVarName);
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new InvalidOperationException($"Environment variable '{aspnetEnvVarName}' is not set or is whitespace.");
        }

        SerilogConfig.Setup(entryAssembly, environment);
        ILogger logger = Log.ForContext(loggerContext);

        try
        {
            logger.Information("Starting in {Environment} environment...", environment);
            hostBuilder.Build().Run();
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Unexpected failure");
        }
        finally
        {
            logger.Information("Ended");
            Log.CloseAndFlush();
        }
    }
}