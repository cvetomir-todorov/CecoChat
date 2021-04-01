using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;

namespace CecoChat.Serilog
{
    public static class SerilogConfiguration
    {
        public static void Setup(Assembly entryAssembly)
        {
            string binPath = Path.GetDirectoryName(entryAssembly.Location);
            Environment.SetEnvironmentVariable("CecoChatLoggingDir", binPath);

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("logging.json", optional: false);

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environment))
            {
                configurationBuilder.AddJsonFile($"logging.{environment}.json", optional: true);
            }

            IConfiguration configuration = configurationBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("Application", entryAssembly.GetName().Name)
                .Enrich.WithSpan()
                .CreateLogger();
        }
    }
}
