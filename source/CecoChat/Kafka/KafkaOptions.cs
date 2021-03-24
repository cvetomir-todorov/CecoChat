using System.Collections.Generic;

namespace CecoChat.Kafka
{
    public interface IKafkaOptions
    {
        List<string> BootstrapServers { get; }
    }

    public sealed class KafkaOptions : IKafkaOptions
    {
        public List<string> BootstrapServers { get; set; }
    }
}
