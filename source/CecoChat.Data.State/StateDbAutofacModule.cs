using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.State.Instrumentation;
using CecoChat.Data.State.Repos;
using CecoChat.Tracing;
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

        string utilityName = $"{nameof(StateActivityUtility)}.{nameof(IActivityUtility)}";
        builder.RegisterType<StateActivityUtility>().As<IStateActivityUtility>()
            .WithNamedParameter(typeof(IActivityUtility), utilityName)
            .SingleInstance();
        builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
    }
}