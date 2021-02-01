using System.Collections.Generic;

namespace CecoChat.Kafka
{
    public interface IKafkaOptions
    {
        List<string> BootstrapServers { get; }

        string ConsumerGroupID { get; set; }
    }

    public sealed class KafkaOptions : IKafkaOptions
    {
        public List<string> BootstrapServers { get; set; }

        public string ConsumerGroupID { get; set; }
    }
}
