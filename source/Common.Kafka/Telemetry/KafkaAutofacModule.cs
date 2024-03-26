using Autofac;

namespace Common.Kafka.Telemetry;

public sealed class KafkaAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<KafkaTelemetry>().As<IKafkaTelemetry>().SingleInstance();
    }
}
