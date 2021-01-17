using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CecoChat.Messaging.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            SetupLogging();
            ILogger logger = Log.ForContext(typeof(Program));

            try
            {
                logger.Information("Starting...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception exception)
            {
                logger.Fatal(exception, "Unexpected failure.");
            }
            finally
            {
                logger.Information("End.");
                Log.CloseAndFlush();
            }
        }

        private static void SetupLogging()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string binPath = Path.GetDirectoryName(currentAssembly.Location);
            Environment.SetEnvironmentVariable("CecoChatLoggingDir", binPath);

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("logging.json", optional: false)
                .AddJsonFile($"logging.{environment}.json", optional: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("Application", currentAssembly.GetName().Name)
                .CreateLogger();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
