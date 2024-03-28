using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Config.Client;

public sealed class ConfigClientAutofacModule : Module
{
    private readonly IConfiguration _configClientConfiguration;

    public ConfigClientAutofacModule(IConfiguration configClientConfiguration)
    {
        _configClientConfiguration = configClientConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ConfigClient>().As<IConfigClient>().SingleInstance();
        builder.RegisterOptions<ConfigClientOptions>(_configClientConfiguration);
    }
}
