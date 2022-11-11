using Autofac;
using CecoChat.Otel;

namespace CecoChat.Kafka.Telemetry;

public sealed class KafkaAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<KafkaTelemetry>().As<IKafkaTelemetry>().SingleInstance();
        builder.RegisterType<OtelTelemetry>().As<ITelemetry>().SingleInstance();
    }
}
