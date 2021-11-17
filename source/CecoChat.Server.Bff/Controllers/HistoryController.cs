using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client.History;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.Bff.Controllers
{
    public sealed class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
    {
        public GetHistoryRequestValidator()
        {
            RuleFor(x => x.OtherUserID).GreaterThan(0);
            RuleFor(x => x.OlderThan).GreaterThan(Snowflake.Epoch);
        }
    }

    [ApiController]
    [Route("api/history")]
    public class HistoryController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IHistoryClient _historyClient;

        public HistoryController(
            ILogger<HistoryController> logger,
            IHistoryClient historyClient)
        {
            _logger = logger;
            _historyClient = historyClient;
        }

        [Authorize(Roles = "user")]
        [HttpGet("messages", Name = "GetMessages")]
        [ProducesResponseType(typeof(GetHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessages([FromQuery][BindRequired] GetHistoryRequest request, CancellationToken ct)
        {
            if (!TryGetUserClaims(HttpContext, out UserClaims userClaims))
            {
                return Unauthorized();
            }
            if (!HttpContext.TryGetBearerAccessTokenValue(out string accessToken))
            {
                return Unauthorized();
            }

            if (request.OlderThan.Kind != DateTimeKind.Utc)
            {
                request.OlderThan = request.OlderThan.ToUniversalTime();
            }
            IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages = await _historyClient.GetHistory(userClaims.UserID, request.OtherUserID, request.OlderThan, accessToken, ct);
            List<HistoryMessage> clientMessages = MapMessages(serviceMessages);

            _logger.LogTrace("Return {0} messages for user {1} and client {2}.", clientMessages.Count, userClaims.UserID, userClaims.ClientID);
            return Ok(new GetHistoryResponse
            {
                Messages = clientMessages
            });
        }

        private List<HistoryMessage> MapMessages(IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages)
        {
            List<HistoryMessage> clientMessages = new(capacity: serviceMessages.Count);

            foreach (Contracts.History.HistoryMessage serviceMessage in serviceMessages)
            {
                HistoryMessage clientMessage = MapMessage(serviceMessage);
                clientMessages.Add(clientMessage);
            }

            return clientMessages;
        }

        private HistoryMessage MapMessage(Contracts.History.HistoryMessage fromService)
        {
            HistoryMessage toClient = new()
            {
                MessageID = fromService.MessageId,
                SenderID = fromService.SenderId,
                ReceiverID = fromService.ReceiverId,
            };

            switch (fromService.DataType)
            {
                case Contracts.History.DataType.PlainText:
                    toClient.DataType = DataType.PlainText;
                    toClient.Data = fromService.Data;
                    break;
                default:
                    throw new EnumValueNotSupportedException(fromService.DataType);
            }

            if (fromService.Reactions != null && fromService.Reactions.Count > 0)
            {
                toClient.Reactions = new Dictionary<long, string>(capacity: fromService.Reactions.Count);

                foreach (KeyValuePair<long,string> reaction in fromService.Reactions)
                {
                    toClient.Reactions.Add(reaction.Key, reaction.Value);
                }
            }

            return toClient;
        }

        private bool TryGetUserClaims(HttpContext context, out UserClaims userClaims)
        {
            if (!context.User.TryGetUserClaims(out userClaims))
            {
                _logger.LogError("Client from was authorized but has no parseable access token.");
                return false;
            }

            Activity.Current?.SetTag("user.id", userClaims.UserID);
            return true;
        }
    }
}