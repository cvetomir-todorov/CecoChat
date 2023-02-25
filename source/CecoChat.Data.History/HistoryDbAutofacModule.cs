using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Cassandra.Telemetry;
using CecoChat.Data.History.Repos;
using CecoChat.Data.History.Telemetry;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.History;

public sealed class HistoryDbAutofacModule : Module
{
    private readonly IConfiguration _historyDbConfiguration;

    public HistoryDbAutofacModule(IConfiguration historyDbConfiguration)
    {
        _historyDbConfiguration = historyDbConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        CassandraAutofacModule<HistoryDbContext, IHistoryDbContext> historyDbModule = new(_historyDbConfiguration);
        builder.RegisterModule(historyDbModule);
        builder.RegisterType<CassandraDbInitializer>().As<ICassandraDbInitializer>()
            .WithNamedParameter(typeof(ICassandraDbContext), historyDbModule.DbContextName)
            .SingleInstance();

        builder.RegisterType<DataMapper>().As<IDataMapper>().SingleInstance();
        builder.RegisterType<ChatMessageRepo>().As<IChatMessageRepo>().SingleInstance();
        builder.RegisterType<HistoryTelemetry>().As<IHistoryTelemetry>().SingleInstance();
        builder.RegisterType<CassandraTelemetry>().As<ICassandraTelemetry>().SingleInstance();
    }
}
