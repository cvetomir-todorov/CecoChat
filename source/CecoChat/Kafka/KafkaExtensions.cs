using CecoChat.Kafka.Instrumentation;
using CecoChat.Tracing;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Kafka
{
    public static class KafkaExtensions
    {
        public static IServiceCollection AddKafka(this IServiceCollection services)
        {
            return services
                .AddSingleton<IKafkaActivityUtility, KafkaActivityUtility>()
                .AddSingleton<IActivityUtility, ActivityUtility>();
        }
    }
}
