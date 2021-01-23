using CecoChat.Contracts.Backend;
using CecoChat.Data.Messaging;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Backend
{
    public sealed class CassandraStateProcessor : IProcessor
    {
        private readonly ILogger _logger;
        private readonly IMessagingRepository _messagingRepository;

        public CassandraStateProcessor(
            ILogger<CassandraStateProcessor> logger,
            IMessagingRepository messagingRepository)
        {
            _logger = logger;
            _messagingRepository = messagingRepository;
        }

        public void Process(Message message)
        {
            _messagingRepository.InsertMessage(message).Wait();
        }
    }
}
