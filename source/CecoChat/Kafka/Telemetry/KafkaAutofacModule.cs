using Autofac;
using CecoChat.Autofac;
using CecoChat.Otel;

namespace CecoChat.Kafka.Telemetry;

public sealed class KafkaAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        string telemetryName = $"{nameof(KafkaTelemetry)}.{nameof(ITelemetry)}";

        builder.RegisterType<KafkaTelemetry>().As<IKafkaTelemetry>()
            .WithNamedParameter(typeof(ITelemetry), telemetryName)
            .SingleInstance();
        builder.RegisterType<OtelTelemetry>().Named<ITelemetry>(telemetryName).SingleInstance();
    }
}