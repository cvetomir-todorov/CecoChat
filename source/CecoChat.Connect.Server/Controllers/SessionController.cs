using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Server.Backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CecoChat.Connect.Server.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IHistoryConfiguration _historyConfiguration;

        public SessionController(
            ILogger<SessionController> logger,
            IPartitionUtility partitionUtility,
            IMessagingConfiguration messagingConfiguration,
            IHistoryConfiguration historyConfiguration)
        {
            _logger = logger;
            _partitionUtility = partitionUtility;
            _messagingConfiguration = messagingConfiguration;
            _historyConfiguration = historyConfiguration;
        }

        [HttpPost]
        public IActionResult CreateSession([FromBody] CreateSessionRequest request)
        {
            int partitionCount = _messagingConfiguration.PartitionCount;
            int partition = _partitionUtility.ChoosePartition(request.UserID, partitionCount);

            CreateSessionResponse response = new()
            {
                MessagingServerAddress = _messagingConfiguration.GetServerAddress(partition),
                HistoryServerAddress = _historyConfiguration.ServerAddress
            };

            _logger.LogTrace("Session for user {0} in partition {1} uses messaging server {2} and history server {3}.",
                request.UserID, partition, response.MessagingServerAddress, response.HistoryServerAddress);
            return Ok(response);
        }
    }
}
