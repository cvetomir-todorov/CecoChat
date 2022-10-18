using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Client.State;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

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
        if (!TryGetUserClaims(HttpContext, out UserClaims userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.State.ChatState> serviceChats = await _client.GetChats(userClaims.UserID, request.NewerThan, accessToken, ct);
        List<ChatState> clientChats = MapChats(serviceChats);

        _logger.LogTrace("Return {0} chats for user {1} and client {2}.", clientChats.Count, userClaims.UserID, userClaims.ClientID);
        return Ok(new GetChatsResponse
        {
            Chats = clientChats
        });
    }

    private static List<ChatState> MapChats(IReadOnlyCollection<Contracts.State.ChatState> serviceChats)
    {
        List<ChatState> clientChats = new();

        foreach (Contracts.State.ChatState serviceChat in serviceChats)
        {
            ChatState clientChat = new()
            {
                ChatID = serviceChat.ChatId,
                NewestMessage = serviceChat.NewestMessage,
                OtherUserDelivered = serviceChat.OtherUserDelivered,
                OtherUserSeen = serviceChat.OtherUserSeen
            };

            clientChats.Add(clientChat);
        }

        return clientChats;
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