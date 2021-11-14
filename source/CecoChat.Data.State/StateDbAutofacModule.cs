using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.State.Instrumentation;
using CecoChat.Data.State.Repos;
using CecoChat.Tracing;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.State
{
    public class StateDbAutofacModule : Module
    {
        public IConfiguration StateDbConfiguration { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            CassandraAutofacModule<StateDbContext, IStateDbContext> stateDbModule = new()
            {
                CassandraConfiguration = StateDbConfiguration
            };
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
}