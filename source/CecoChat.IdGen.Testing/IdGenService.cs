using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using CecoChat.IdGen.Service;
using CecoChat.Server;
using CecoChat.Testing.Config;
using Common.Autofac;
using Common.Kafka;
using Common.Testing.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        builder.WebHost.UseKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, listenPort, listenOptions =>
            {
                listenOptions.UseHttps(certificatePath, certificatePassword);
            });
        });
        builder.Host.UseSerilog(dispose: true);

        CommonOptions commonOptions = new(builder.Configuration);

        Program.AddServices(builder, commonOptions);
        Program.AddHealth(builder, commonOptions);
        Program.AddTelemetry(builder, commonOptions);

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
        Program.ConfigurePipeline(_app);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(timeout: TimeSpan.FromSeconds(5));
        await _app.DisposeAsync();
    }

    public async Task Run()
    {
        await _app.StartAsync();
    }
}
