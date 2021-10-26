using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.State.Repos;
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
        }
    }
}