using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Common.AspNet.Init;
using Common.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace CecoChat.Server;

public static class EntryPoint
{
    private const string EnvironmentVariablesPrefix = "CECOCHAT_";

    public static async Task<int> Run(string[] args, Type loggerContext, Action<WebApplicationBuilder> configureBuilder, Action<WebApplication> configurePipeline)
    {
        WebApplication? app = null;
        await using Logger fallbackLogger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        ILogger logger = fallbackLogger;

        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables(EnvironmentVariablesPrefix);
            // command line args over env vars
            builder.Configuration.AddCommandLine(args);
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            Assembly entryAssembly = GetEntryAssembly();
            SetupSerilog(builder.Configuration, builder.Environment.EnvironmentName, entryAssembly);
            logger = Log.ForContext(loggerContext);

            logger.Information("Starting in {Environment} environment...", builder.Environment.EnvironmentName);

            builder.Host.UseSerilog(dispose: false);

            configureBuilder(builder);

            app = builder.Build();
            configurePipeline(app);

            await app.RunAsync();
            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.Information("Starting cancelled");
            return 0;
        }
        catch (InitException initException)
        {
            logger.Fatal(initException, "Failed to initialize");
            return 1;
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

    private static void SetupSerilog(IConfiguration configuration, string environment, Assembly entryAssembly)
    {
        OtlpLoggingOptions otlpOptions = new();
        configuration.GetSection("Telemetry:Logging:Export").Bind(otlpOptions);

        SerilogConfig.Setup(entryAssembly, environment, otlpOptions);
    }
}
