using Autofac;
using CecoChat.Autofac;
using CecoChat.Tracing;

namespace CecoChat.Kafka.Instrumentation
{
    public sealed class KafkaInstrumentationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            string utilityName = $"{nameof(KafkaActivityUtility)}.{nameof(IActivityUtility)}";

            builder.RegisterType<KafkaActivityUtility>().As<IKafkaActivityUtility>()
                .WithNamedParameter(typeof(IActivityUtility), utilityName)
                .SingleInstance();
            builder.RegisterType<ActivityUtility>().Named<IActivityUtility>(utilityName).SingleInstance();
        }
    }
}
