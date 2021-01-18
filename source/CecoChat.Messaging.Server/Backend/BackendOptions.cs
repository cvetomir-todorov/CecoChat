using System.Collections.Generic;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendOptions
    {
        public List<string> BootstrapServers { get; }

        public string MessagesTopicName { get; }

        public int MessagesTopicPartitionCount { get; }

        public string ConsumerGroupID { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public List<string> BootstrapServers { get; set; }

        public string MessagesTopicName { get; set; }

        public int MessagesTopicPartitionCount { get; set; }

        public string ConsumerGroupID { get; set; }
    }
}
