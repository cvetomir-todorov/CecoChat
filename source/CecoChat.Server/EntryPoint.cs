using System;
using System.Reflection;
using CecoChat.Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CecoChat.Server
{
    public static class EntryPoint
    {
        public static IHostBuilder CreateDefaultHostBuilder(
            string[] args,
            Type startupContext,
            bool useSerilog = true,
            string environmentVariablesPrefix = "CECOCHAT_")
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

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
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            SerilogConfiguration.Setup(Assembly.GetEntryAssembly(), environment);
            ILogger logger = Log.ForContext(loggerContext);

            try
            {
                logger.Information("Starting...");
                hostBuilder.Build().Run();
            }
            catch (Exception exception)
            {
                logger.Fatal(exception, "Unexpected failure.");
            }
            finally
            {
                logger.Information("Ended.");
                Log.CloseAndFlush();
            }
        }
    }
}
