using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Data.History.Repos;
using CecoChat.Tracing;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.History
{
    public sealed class HistoryDbAutofacModule : Module
    {
        public IConfiguration HistoryDbConfiguration { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            CassandraAutofacModule<HistoryDbContext, IHistoryDbContext> historyDbModule = new()
            {
                CassandraConfiguration = HistoryDbConfiguration
            };
            builder.RegisterModule(historyDbModule);
            builder.RegisterType<CassandraDbInitializer>().As<ICassandraDbInitializer>()
                .WithNamedParameter(typeof(ICassandraDbContext), historyDbModule.DbContextName)
                .SingleInstance();

            builder.RegisterType<DataUtility>().As<IDataUtility>().SingleInstance();
            builder.RegisterType<DataMapper>().As<IDataMapper>().SingleInstance();

            builder.RegisterType<HistoryRepo>().As<IHistoryRepo>().SingleInstance();
            builder.RegisterType<ReactionRepo>().As<IReactionRepo>().SingleInstance();
            builder.RegisterType<NewMessageRepo>().As<INewMessageRepo>().SingleInstance();

            string utilityName = $"{nameof(HistoryActivityUtility)}.{nameof(IActivityUtility)}";

            builder.RegisterType<HistoryActivityUtility>().As<IHistoryActivityUtility>()
                .WithNamedParameter(typeof(IActivityUtility), utilityName)
                .SingleInstance();
            builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
        }
    }
}
