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
    private readonly IContractMapper _contractMapper;
    private readonly IHistoryClient _historyClient;

    public HistoryController(
        ILogger<HistoryController> logger,
        IContractMapper contractMapper,
        IHistoryClient historyClient)
    {
        _logger = logger;
        _contractMapper = contractMapper;
        _historyClient = historyClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet("messages", Name = "GetMessages")]
    [ProducesResponseType(typeof(GetHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMessages([FromQuery][BindRequired] GetHistoryRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages = await _historyClient.GetHistory(userClaims.UserId, request.OtherUserId, request.OlderThan, accessToken, ct);
        HistoryMessage[] clientMessages = serviceMessages.Select(message => _contractMapper.MapMessage(message)).ToArray();

        _logger.LogTrace("Responding with {MessageCount} message(s) for chat between {UserId} and {OtherUserId} older than {OlderThan}",
            clientMessages.Length, userClaims.UserId, request.OtherUserId, request.OlderThan);
        return Ok(new GetHistoryResponse
        {
            Messages = clientMessages
        });
    }
}
