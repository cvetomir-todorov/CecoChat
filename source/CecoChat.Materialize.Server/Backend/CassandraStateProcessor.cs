using CecoChat.Contracts.Backend;
using CecoChat.Data.Messaging;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Backend
{
    public sealed class CassandraStateProcessor : IProcessor
    {
        private readonly ILogger _logger;
        private readonly INewMessageRepository _newMessageRepository;

        public CassandraStateProcessor(
            ILogger<CassandraStateProcessor> logger,
            INewMessageRepository newMessageRepository)
        {
            _logger = logger;
            _newMessageRepository = newMessageRepository;
        }

        public void Process(Message message)
        {
            _newMessageRepository.InsertMessage(message).Wait();
        }
    }
}
