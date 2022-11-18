using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CecoChat.Client.State;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Clients.State;

[ApiController]
[Route("api/state")]
public class StateController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IStateClient _client;

    public StateController(
        ILogger<StateController> logger,
        IStateClient client)
    {
        _logger = logger;
        _client = client;
    }

    [Authorize(Roles = "user")]
    [HttpGet("chats", Name = "GetChats")]
    [ProducesResponseType(typeof(GetChatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetChats([FromQuery][BindRequired] GetChatsRequest request, CancellationToken ct)
    {
        if (!TryGetUserClaims(HttpContext, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.State.ChatState> serviceChats = await _client.GetChats(userClaims.UserId, request.NewerThan, accessToken, ct);
        ChatState[] clientChats = serviceChats.Select(MapChat).ToArray();

        _logger.LogTrace("Responding with {ChatCount} chats for user {UserId} which are newer than {NewerThan}",
            clientChats.Length, userClaims.UserId, request.NewerThan);
        return Ok(new GetChatsResponse
        {
            Chats = clientChats
        });
    }

    private static ChatState MapChat(Contracts.State.ChatState fromService)
    {
        return new ChatState
        {
            ChatID = fromService.ChatId,
            NewestMessage = fromService.NewestMessage,
            OtherUserDelivered = fromService.OtherUserDelivered,
            OtherUserSeen = fromService.OtherUserSeen
        };
    }

    private bool TryGetUserClaims(HttpContext context, [NotNullWhen(true)] out UserClaims? userClaims)
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            _logger.LogError("Client from was authorized but has no parseable access token");
            return false;
        }

        Activity.Current?.SetTag("user.id", userClaims.UserId);
        return true;
    }
}
