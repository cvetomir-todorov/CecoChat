using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Server.Backend;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CecoChat.Connect.Server.Controllers
{
    [ApiController]
    [Route("api/connect")]
    public class ConnectController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IHistoryConfiguration _historyConfiguration;

        public ConnectController(
            ILogger<ConnectController> logger,
            IPartitionUtility partitionUtility,
            IMessagingConfiguration messagingConfiguration,
            IHistoryConfiguration historyConfiguration)
        {
            _logger = logger;
            _partitionUtility = partitionUtility;
            _messagingConfiguration = messagingConfiguration;
            _historyConfiguration = historyConfiguration;
        }

        [Authorize(Roles = "user")]
        [HttpGet]
        public IActionResult Connect()
        {
            if (!HttpContext.User.TryGetUserID(out long userID))
            {
                return Unauthorized();
            }

            int partitionCount = _messagingConfiguration.PartitionCount;
            int partition = _partitionUtility.ChoosePartition(userID, partitionCount);

            ConnectResponse response = new()
            {
                MessagingServerAddress = _messagingConfiguration.GetServerAddress(partition),
                HistoryServerAddress = _historyConfiguration.ServerAddress
            };

            _logger.LogTrace("User {0} in partition {1} uses messaging server {2} and history server {3}.",
                userID, partition, response.MessagingServerAddress, response.HistoryServerAddress);
            return Ok(response);
        }
    }
}
