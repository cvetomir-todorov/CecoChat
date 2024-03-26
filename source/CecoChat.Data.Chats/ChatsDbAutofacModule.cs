using Autofac;
using CecoChat.Cassandra;
using CecoChat.Cassandra.Telemetry;
using CecoChat.Data.Chats.Entities.ChatMessages;
using CecoChat.Data.Chats.Entities.UserChats;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Chats;

public sealed class ChatsDbAutofacModule : Module
{
    private readonly IConfiguration _chatsDbConfiguration;

    public ChatsDbAutofacModule(IConfiguration chatsDbConfiguration)
    {
        _chatsDbConfiguration = chatsDbConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // chat messages
        builder.RegisterType<DataMapper>().As<IDataMapper>().SingleInstance();
        builder.RegisterType<ChatMessageRepo>().As<IChatMessageRepo>().SingleInstance();
        builder.RegisterType<ChatMessageTelemetry>().As<IChatMessageTelemetry>().SingleInstance();

        // user chats
        builder.RegisterType<UserChatsRepo>().As<IUserChatsRepo>().SingleInstance();
        builder.RegisterType<UserChatsTelemetry>().As<IUserChatsTelemetry>().SingleInstance();

        // db
        CassandraAutofacModule<ChatsDbContext, IChatsDbContext> chatsDbModule = new(_chatsDbConfiguration);
        builder.RegisterModule(chatsDbModule);
        builder.RegisterType<CassandraDbInitializer>().As<ICassandraDbInitializer>()
            .WithNamedParameter(typeof(ICassandraDbContext), chatsDbModule.DbContextName)
            .SingleInstance();
        builder.RegisterType<CassandraTelemetry>().As<ICassandraTelemetry>().SingleInstance();
    }
}
