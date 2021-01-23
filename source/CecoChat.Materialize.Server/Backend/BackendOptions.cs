using System.Collections.Generic;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IBackendOptions
    {
        public List<string> BootstrapServers { get; }

        public string MessagesTopicName { get; }

        public string ConsumerGroupID { get; }
    }

    public sealed class BackendOptions : IBackendOptions
    {
        public List<string> BootstrapServers { get; set; }

        public string MessagesTopicName { get; set; }

        public string ConsumerGroupID { get; set; }
    }
}
