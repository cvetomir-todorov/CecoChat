using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Chats.Client;

public sealed class ChatsClientAutofacModule : Module
{
    private readonly IConfiguration _chatsClientConfiguration;

    public ChatsClientAutofacModule(IConfiguration chatsClientConfiguration)
    {
        _chatsClientConfiguration = chatsClientConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ChatsClient>().As<IChatsClient>().SingleInstance();
        builder.RegisterOptions<ChatsClientOptions>(_chatsClientConfiguration);
    }
}
