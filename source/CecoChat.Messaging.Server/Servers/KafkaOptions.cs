using System.Collections.Generic;

namespace CecoChat.Messaging.Server.Servers
{
    public interface IKafkaOptions
    {
        public List<string> BootstrapServers { get; }

        public string MessagesTopic { get; }

        public int MessagesTopicPartitionCount { get; }

        public string ConsumerGroupID { get; }
    }

    public sealed class KafkaOptions : IKafkaOptions
    {
        public List<string> BootstrapServers { get; set; }

        public string MessagesTopic { get; set; }

        public int MessagesTopicPartitionCount { get; set; }

        public string ConsumerGroupID { get; set; }
    }
}
