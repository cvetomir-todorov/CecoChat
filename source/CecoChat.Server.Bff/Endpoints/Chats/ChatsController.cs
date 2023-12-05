using CecoChat.Client.Chats;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Chats;

[ApiController]
[Route("api/chats")]
[ApiExplorerSettings(GroupName = "Chats")]
public class ChatsController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IContractMapper _contractMapper;
    private readonly IChatsClient _chatsClient;

    public ChatsController(
        ILogger<ChatsController> logger,
        IContractMapper contractMapper,
        IChatsClient chatsClient)
    {
        _logger = logger;
        _contractMapper = contractMapper;
        _chatsClient = chatsClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet("history", Name = "GetChatHistory")]
    [ProducesResponseType(typeof(GetHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetChatHistory([FromQuery][BindRequired] GetHistoryRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.Chats.HistoryMessage> serviceMessages = await _chatsClient.GetHistory(userClaims.UserId, request.OtherUserId, request.OlderThan, accessToken, ct);
        HistoryMessage[] clientMessages = serviceMessages.Select(message => _contractMapper.MapMessage(message)).ToArray();

        _logger.LogTrace("Responding with {MessageCount} message(s) for chat between {UserId} and {OtherUserId} older than {OlderThan}",
            clientMessages.Length, userClaims.UserId, request.OtherUserId, request.OlderThan);
        return Ok(new GetHistoryResponse
        {
            Messages = clientMessages
        });
    }

    [Authorize(Policy = "user")]
    [HttpGet("state", Name = "GetUserChats")]
    [ProducesResponseType(typeof(GetChatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserChats([FromQuery][BindRequired] GetChatsRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.Chats.ChatState> serviceChats = await _chatsClient.GetUserChats(userClaims.UserId, request.NewerThan, accessToken, ct);
        ChatState[] clientChats = serviceChats.Select(chat => _contractMapper.MapChat(chat)).ToArray();

        _logger.LogTrace("Responding with {ChatCount} chats for user {UserId} which are newer than {NewerThan}",
            clientChats.Length, userClaims.UserId, request.NewerThan);
        return Ok(new GetChatsResponse
        {
            Chats = clientChats
        });
    }
}
