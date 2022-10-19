using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.History;

public sealed class HistoryClientAutofacModule : Module
{
    private readonly IConfiguration _historyClientConfiguration;

    public HistoryClientAutofacModule(IConfiguration historyClientConfiguration)
    {
        _historyClientConfiguration = historyClientConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<HistoryClient>().As<IHistoryClient>().SingleInstance();
        builder.RegisterOptions<HistoryOptions>(_historyClientConfiguration);
    }
}