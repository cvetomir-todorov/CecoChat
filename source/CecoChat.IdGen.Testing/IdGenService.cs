using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using CecoChat.IdGen.Service;
using CecoChat.Server;
using CecoChat.Testing.Config;
using Common.AspNet.Init;
using Common.Autofac;
using Common.Kafka;
using Common.Testing.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Serilog;

namespace CecoChat.IdGen.Testing;

public sealed class IdGenService : IAsyncDisposable
{
    private readonly WebApplication _app;

    public IdGenService(string environment, int listenPort, string certificatePath, string certificatePassword, string configFilePath)
    {
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = environment
        });
        builder.Configuration.AddJsonFile(configFilePath, optional: false);
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, listenPort, listenOptions =>
            {
                listenOptions.UseHttps(certificatePath, certificatePassword);
            });
        });
        builder.Host.UseSerilog(dispose: true);

        CommonOptions options = new(builder.Configuration);

        Program.AddServices(builder, options);
        Program.AddHealth(builder, options);
        Program.AddTelemetry(builder, options);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>((host, autofacBuilder) =>
        {
            Program.ConfigureContainer(host, autofacBuilder);

            // override registrations
            autofacBuilder.Register(_ => new ConfigClientStub(
                [
                    new() { Name = ConfigKeys.Snowflake.GeneratorIds, Value = "123=0,1" }
                ]))
                .As<IConfigClient>().SingleInstance();
            autofacBuilder.RegisterType<KafkaAdminDummy>().As<IKafkaAdmin>().SingleInstance();
            autofacBuilder.RegisterFactory<KafkaConsumerDummy<Null, ConfigChange>, IKafkaConsumer<Null, ConfigChange>>();
        });

        _app = builder.Build();
        Program.ConfigurePipeline(_app, options);
    }

    public async Task Run()
    {
        bool initialized = await _app.Services.Init();
        if (!initialized)
        {
            await TestContext.Progress.WriteLineAsync("Failed to initialize");
            return;
        }

        _ = _app
            .RunAsync()
            .ContinueWith(task => TestContext.Progress.WriteLine($"Unexpected error occurred: {task.Exception}"), TaskContinuationOptions.OnlyOnFaulted)
            .ContinueWith(_ => TestContext.Progress.WriteLine("Ended successfully"), TaskContinuationOptions.NotOnFaulted);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(timeout: TimeSpan.FromSeconds(5));
        await _app.DisposeAsync();
    }
}
