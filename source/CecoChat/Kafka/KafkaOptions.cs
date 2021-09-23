using System.Collections.Generic;

namespace CecoChat.Kafka
{
    public sealed class KafkaOptions
    {
        public List<string> BootstrapServers { get; set; }
    }
}
