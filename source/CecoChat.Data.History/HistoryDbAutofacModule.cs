using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Data.History.Repos;
using CecoChat.Data.History.Telemetry;
using CecoChat.Otel;
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

        string telemetryName = $"{nameof(HistoryTelemetry)}.{nameof(ITelemetry)}";
        builder.RegisterType<HistoryTelemetry>().As<IHistoryTelemetry>()
            .WithNamedParameter(typeof(ITelemetry), telemetryName)
            .SingleInstance();
        builder.RegisterType<OtelTelemetry>().Named<ITelemetry>(telemetryName).SingleInstance();
    }
}