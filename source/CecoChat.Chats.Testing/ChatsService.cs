using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.Backplane.Contracts;
using CecoChat.Chats.Data;
using CecoChat.Chats.Data.Entities.ChatMessages;
using CecoChat.Chats.Data.Entities.UserChats;
using CecoChat.Chats.Service;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using CecoChat.Server;
using CecoChat.Testing.Config;
using Common.Autofac;
using Common.Cassandra;
using Common.Jwt;
using Common.Kafka;
using Common.Testing.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CecoChat.Chats.Testing;

public sealed class ChatsService : IAsyncDisposable
{
    private readonly WebApplication _app;

    public ChatsService(string environment, int listenPort, string certificatePath, string certificatePassword, string configFilePath, IChatsDb chatsDb)
    {
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = environment
        });
        builder.Configuration.AddJsonFile(configFilePath, optional: false);
        builder.Services.Configure<CassandraOptions<IChatsDbContext>>(cassandra =>
        {
            cassandra.ContactPoints = [$"{chatsDb.Host}:{chatsDb.Port}"];
        });
        builder.WebHost.UseKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, listenPort, listenOptions =>
            {
                listenOptions.UseHttps(certificatePath, certificatePassword);
            });
        });
        builder.Host.UseSerilog(dispose: false);

        CommonOptions commonOptions = new(builder.Configuration);
        CassandraOptions chatsDbOptions = new();
        builder.Configuration.GetSection("ChatsDb:Cluster").Bind(chatsDbOptions);
        chatsDbOptions.ContactPoints = [$"{chatsDb.Host}:{chatsDb.Port}"];

        Program.AddServices(builder, commonOptions);
        Program.AddHealth(builder, commonOptions, chatsDbOptions);
        Program.AddTelemetry(builder, commonOptions);

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>((host, autofacBuilder) =>
        {
            Program.ConfigureContainer(host, autofacBuilder);

            // override registrations
            autofacBuilder.Register(_ => new ConfigClientStub(
                [
                    new() { Name = ConfigKeys.History.MessageCount, Value = "4" }
                ]))
                .As<IConfigClient>().SingleInstance();
            autofacBuilder.RegisterType<KafkaAdminDummy>().As<IKafkaAdmin>().SingleInstance();
            autofacBuilder.RegisterFactory<KafkaConsumerDummy<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
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

    public JwtOptions GetJwtOptions()
    {
        JwtOptions jwtOptions = new();
        _app.Configuration.GetSection("Jwt").Bind(jwtOptions);

        return jwtOptions;
    }

    public IUserChatsRepo UserChats()
    {
        return _app.Services.GetRequiredService<IUserChatsRepo>();
    }

    public IChatMessageRepo ChatMessages()
    {
        return _app.Services.GetRequiredService<IChatMessageRepo>();
    }
}
