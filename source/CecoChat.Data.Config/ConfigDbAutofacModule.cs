using Autofac;
using CecoChat.Npgsql;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Config;

public sealed class ConfigDbAutofacModule : Module
{
    private readonly IConfiguration _configDbConfiguration;

    public ConfigDbAutofacModule(IConfiguration configDbConfiguration)
    {
        _configDbConfiguration = configDbConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        builder.RegisterOptions<ConfigDbOptions>(_configDbConfiguration);
    }
}
