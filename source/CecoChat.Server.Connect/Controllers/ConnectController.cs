using System.Diagnostics;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.Connect.Controllers
{
    [ApiController]
    [Route("api/connect")]
    public class ConnectController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IPartitioningConfig _partitioningConfig;
        private readonly IHistoryConfig _historyConfig;

        public ConnectController(
            ILogger<ConnectController> logger,
            IPartitionUtility partitionUtility,
            IPartitioningConfig partitioningConfig,
            IHistoryConfig historyConfig)
        {
            _logger = logger;
            _partitionUtility = partitionUtility;
            _partitioningConfig = partitioningConfig;
            _historyConfig = historyConfig;
        }

        [Authorize(Roles = "user")]
        [HttpGet]
        public IActionResult Connect()
        {
            if (!HttpContext.User.TryGetUserID(out long userID))
            {
                return Unauthorized();
            }
            Activity.Current?.AddTag("user.id", userID);

            int partitionCount = _partitioningConfig.PartitionCount;
            int partition = _partitionUtility.ChoosePartition(userID, partitionCount);

            ConnectResponse response = new()
            {
                MessagingServerAddress = _partitioningConfig.GetServerAddress(partition),
                HistoryServerAddress = _historyConfig.ServerAddress
            };

            _logger.LogInformation("User {0} in partition {1} uses messaging server {2} and history server {3}.",
                userID, partition, response.MessagingServerAddress, response.HistoryServerAddress);
            return Ok(response);
        }
    }
}
