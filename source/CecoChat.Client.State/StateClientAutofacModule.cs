using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.State;

public sealed class StateClientAutofacModule : Module
{
    public IConfiguration StateClientConfiguration { get; init; }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StateClient>().As<IStateClient>().SingleInstance();
        builder.RegisterOptions<StateOptions>(StateClientConfiguration);
    }
}