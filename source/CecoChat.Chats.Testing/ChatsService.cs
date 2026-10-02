using Autofac;
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
using Common.Testing.AspNet;
using Common.Testing.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Chats.Testing;

public sealed class ChatsService : BaseService
{
    public ChatsService(ServiceOptions options, IChatsDb chatsDb)
    {
        WebApplicationBuilder builder = CreateBuilder(typeof(Program), options);

        string[] chatsDbContactPoints = [$"{chatsDb.Host}:{chatsDb.Port}"];

        builder.Services.Configure<CassandraOptions<IChatsDbContext>>(cassandra =>
        {
            cassandra.ContactPoints = chatsDbContactPoints;
        });

        CommonOptions commonOptions = new(builder.Configuration);

        CassandraOptions chatsDbOptions = new();
        builder.Configuration.GetSection("ChatsDb:Cluster").Bind(chatsDbOptions);
        chatsDbOptions.ContactPoints = chatsDbContactPoints;

        Program.AddServices(builder, commonOptions);
        Program.AddHealth(builder, commonOptions, chatsDbOptions);
        Program.AddTelemetry(builder, commonOptions);

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

        App = builder.Build();
        Program.ConfigurePipeline(App);
    }

    public JwtOptions GetJwtOptions()
    {
        JwtOptions jwtOptions = new();
        App.Configuration.GetSection("Jwt").Bind(jwtOptions);

        return jwtOptions;
    }

    public IUserChatsRepo UserChats()
    {
        return App.Services.GetRequiredService<IUserChatsRepo>();
    }

    public IChatMessageRepo ChatMessages()
    {
        return App.Services.GetRequiredService<IChatMessageRepo>();
    }
}
