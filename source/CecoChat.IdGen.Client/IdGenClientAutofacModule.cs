using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.IdGen.Client;

public sealed class IdGenClientAutofacModule : Module
{
    private readonly IConfiguration _idGenConfiguration;

    public IdGenClientAutofacModule(IConfiguration idGenConfiguration)
    {
        _idGenConfiguration = idGenConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<IdGenClient>().As<IIdGenClient>().SingleInstance();
        builder.RegisterOptions<IdGenClientOptions>(_idGenConfiguration);
    }
}
