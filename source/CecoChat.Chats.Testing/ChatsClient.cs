using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.Chats.Client;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CecoChat.Chats.Testing;

public sealed class ChatsClient : IDisposable
{
    private readonly IChatsClient _instance;
    private readonly IContainer _autofacServices;

    public ChatsClient(string configFilePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(configFilePath, optional: false)
            .Build();
        IConfiguration chatsClientConfig = configuration.GetSection("ChatsClient");

        ServiceCollection services = new();
        ChatsClientOptions chatsClientOptions = new();
        chatsClientConfig.Bind(chatsClientOptions);
        services.AddChatsClient(chatsClientOptions);
        services.AddSerilog(dispose: false);

        ContainerBuilder autofacBuilder = new();
        autofacBuilder.RegisterModule(new ChatsClientAutofacModule(chatsClientConfig));
        autofacBuilder.Populate(services);
        autofacBuilder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
        _autofacServices = autofacBuilder.Build();

        IServiceProvider serviceProvider = new AutofacServiceProvider(_autofacServices);
        _instance = serviceProvider.GetRequiredService<IChatsClient>();
    }

    public void Dispose()
    {
        _autofacServices.Dispose();
    }

    public IChatsClient Instance => _instance;
}
