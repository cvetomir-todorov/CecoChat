using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.IDGen;

public sealed class IDGenAutofacModule : Module
{
    private readonly IConfiguration _idGenConfiguration;

    public IDGenAutofacModule(IConfiguration idGenConfiguration)
    {
        _idGenConfiguration = idGenConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<IDGenClient>().As<IIDGenClient>().SingleInstance();
        builder.RegisterType<IDChannel>().As<IIDChannel>().SingleInstance();
        builder.RegisterOptions<IDGenOptions>(_idGenConfiguration);
    }
}