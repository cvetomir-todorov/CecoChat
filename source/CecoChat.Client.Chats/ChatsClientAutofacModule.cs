using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.Chats;

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
