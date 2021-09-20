using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Tracing;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.History
{
    public sealed class HistoryDbAutofacModule : Module
    {
        public IConfiguration HistoryDbConfiguration { get; init; }

        public bool RegisterHistory { get; init; }

        public bool RegisterNewMessage { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            if (!RegisterHistory && !RegisterNewMessage)
            {
                return;
            }

            builder.RegisterModule(new CassandraAutofacModule<HistoryDbContext, IHistoryDbContext>
            {
                CassandraConfiguration = HistoryDbConfiguration
            });
            builder.RegisterType<HistoryDbInitializer>().As<IHistoryDbInitializer>().SingleInstance();
            builder.RegisterType<DataUtility>().As<IDataUtility>().SingleInstance();
            builder.RegisterType<MessageMapper>().As<IMessageMapper>().SingleInstance();

            if (RegisterHistory)
            {
                builder.RegisterType<HistoryRepository>().As<IHistoryRepository>().SingleInstance();
            }
            if (RegisterNewMessage)
            {
                builder.RegisterType<NewMessageRepository>().As<INewMessageRepository>().SingleInstance();
            }

            string utilityName = $"{nameof(HistoryActivityUtility)}.{nameof(IActivityUtility)}";

            builder.RegisterType<HistoryActivityUtility>().As<IHistoryActivityUtility>()
                .WithNamedParameter(typeof(IActivityUtility), utilityName)
                .SingleInstance();
            builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
        }
    }
}
