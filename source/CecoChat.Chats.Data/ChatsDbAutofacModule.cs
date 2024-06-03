using Autofac;
using CecoChat.Chats.Data.Entities.ChatMessages;
using CecoChat.Chats.Data.Entities.UserChats;
using Common.Autofac;
using Common.Cassandra;
using Common.Cassandra.Telemetry;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Chats.Data;

public sealed class ChatsDbAutofacModule : Module
{
    private readonly IConfiguration _clusterConfiguration;
    private readonly IConfiguration _chatMessagesOperationsConfiguration;
    private readonly IConfiguration _userChatsOperationsConfiguration;

    public ChatsDbAutofacModule(
        IConfiguration clusterConfiguration,
        IConfiguration chatMessagesOperationsConfiguration,
        IConfiguration userChatsOperationsConfiguration)
    {
        _clusterConfiguration = clusterConfiguration;
        _chatMessagesOperationsConfiguration = chatMessagesOperationsConfiguration;
        _userChatsOperationsConfiguration = userChatsOperationsConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // chat messages
        builder.RegisterType<DataMapper>().As<IDataMapper>().SingleInstance();
        builder.RegisterType<ChatMessageRepo>().As<IChatMessageRepo>().SingleInstance();
        builder.RegisterType<ChatMessageTelemetry>().As<IChatMessageTelemetry>().SingleInstance();
        builder.RegisterOptions<ChatMessagesOperationOptions>(_chatMessagesOperationsConfiguration);

        // user chats
        builder.RegisterType<UserChatsRepo>().As<IUserChatsRepo>().SingleInstance();
        builder.RegisterType<UserChatsTelemetry>().As<IUserChatsTelemetry>().SingleInstance();
        builder.RegisterOptions<UserChatsOperationOptions>(_userChatsOperationsConfiguration);

        // db
        CassandraAutofacModule<ChatsDbContext, IChatsDbContext> chatsDbModule = new(_clusterConfiguration);
        builder.RegisterModule(chatsDbModule);
        builder
            .RegisterType<CassandraDbInitializer>()
            .As<ICassandraDbInitializer>()
            .WithNamedParameter(typeof(ICassandraDbContext), chatsDbModule.DbContextName)
            .SingleInstance();
        builder.RegisterType<CassandraTelemetry>().As<ICassandraTelemetry>().SingleInstance();
    }
}
