using CecoChat.Client.History;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints;

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
        if (!HttpContext.TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages = await _historyClient.GetHistory(userClaims.UserId, request.OtherUserId, request.OlderThan, accessToken, ct);
        HistoryMessage[] clientMessages = serviceMessages.Select(MapMessage).ToArray();

        _logger.LogTrace("Responding with {MessageCount} messages for chat between {UserId} and {OtherUserId} older than {OlderThan}",
            clientMessages.Length, userClaims.UserId, request.OtherUserId, request.OlderThan);
        return Ok(new GetHistoryResponse
        {
            Messages = clientMessages
        });
    }

    private static HistoryMessage MapMessage(Contracts.History.HistoryMessage fromService)
    {
        HistoryMessage toClient = new()
        {
            MessageId = fromService.MessageId,
            SenderId = fromService.SenderId,
            ReceiverId = fromService.ReceiverId,
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

            foreach (KeyValuePair<long, string> reaction in fromService.Reactions)
            {
                toClient.Reactions.Add(reaction.Key, reaction.Value);
            }
        }

        return toClient;
    }
}
