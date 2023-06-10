using CecoChat.Client.State;
using CecoChat.Contracts.Bff;
using CecoChat.Server.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints;

public sealed class GetChatsRequestValidator : AbstractValidator<GetChatsRequest>
{
    public GetChatsRequestValidator()
    {
        RuleFor(x => x.NewerThan).ValidNewerThanDateTime();
    }
}

[ApiController]
[Route("api/state")]
public class StateController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IContractMapper _contractMapper;
    private readonly IStateClient _client;

    public StateController(
        ILogger<StateController> logger,
        IContractMapper contractMapper,
        IStateClient client)
    {
        _logger = logger;
        _contractMapper = contractMapper;
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
        if (!HttpContext.TryGetUserClaims(_logger, out UserClaims? userClaims))
        {
            return Unauthorized();
        }
        if (!HttpContext.TryGetBearerAccessTokenValue(out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.State.ChatState> serviceChats = await _client.GetChats(userClaims.UserId, request.NewerThan, accessToken, ct);
        ChatState[] clientChats = serviceChats.Select(chat => _contractMapper.MapChat(chat)).ToArray();

        _logger.LogTrace("Responding with {ChatCount} chats for user {UserId} which are newer than {NewerThan}",
            clientChats.Length, userClaims.UserId, request.NewerThan);
        return Ok(new GetChatsResponse
        {
            Chats = clientChats
        });
    }
}
