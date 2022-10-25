using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.State.Repos;
using CecoChat.Data.State.Telemetry;
using CecoChat.Otel;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.State;

public class StateDbAutofacModule : Module
{
    private readonly IConfiguration _stateDbConfiguration;

    public StateDbAutofacModule(IConfiguration stateDbConfiguration)
    {
        _stateDbConfiguration = stateDbConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        CassandraAutofacModule<StateDbContext, IStateDbContext> stateDbModule = new(_stateDbConfiguration);
        builder.RegisterModule(stateDbModule);
        builder.RegisterType<CassandraDbInitializer>().As<ICassandraDbInitializer>()
            .WithNamedParameter(typeof(ICassandraDbContext), stateDbModule.DbContextName)
            .SingleInstance();

        builder.RegisterType<ChatStateRepo>().As<IChatStateRepo>().SingleInstance();

        string telemetryName = $"{nameof(StateTelemetry)}.{nameof(ITelemetry)}";
        builder.RegisterType<StateTelemetry>().As<IStateTelemetry>()
            .WithNamedParameter(typeof(ITelemetry), telemetryName)
            .SingleInstance();
        builder.RegisterType<OtelTelemetry>().Named<ITelemetry>(telemetryName).SingleInstance();
    }
}