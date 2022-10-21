using System.Diagnostics;
using CecoChat.Client.State;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Controllers;

public sealed class GetChatsRequestValidator : AbstractValidator<GetChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan).GreaterThanOrEqualTo(Snowflake.Epoch);
    }
}

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

        IReadOnlyCollection<Contracts.State.ChatState> serviceChats = await _client.GetChats(userClaims!.UserID, request.NewerThan, accessToken!, ct);
        ChatState[] clientChats = serviceChats.Select(MapChat).ToArray();

        _logger.LogTrace("Return {ChatCount} chats for user {UserId} and client {ClientId}", clientChats.Length, userClaims.UserID, userClaims.ClientID);
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

    private bool TryGetUserClaims(HttpContext context, out UserClaims? userClaims)
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            _logger.LogError("Client from was authorized but has no parseable access token");
            return false;
        }

        Activity.Current?.SetTag("user.id", userClaims!.UserID);
        return true;
    }
}