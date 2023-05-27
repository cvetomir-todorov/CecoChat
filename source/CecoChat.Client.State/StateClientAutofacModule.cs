using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.State;

public sealed class StateClientAutofacModule : Module
{
    private readonly IConfiguration _stateClientConfiguration;

    public StateClientAutofacModule(IConfiguration stateClientConfiguration)
    {
        _stateClientConfiguration = stateClientConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StateClient>().As<IStateClient>().SingleInstance();
        builder.RegisterOptions<StateOptions>(_stateClientConfiguration);
    }
}
