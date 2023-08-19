using CecoChat.Client.State;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Chats;

[ApiController]
[Route("api/state")]
[ApiExplorerSettings(GroupName = "Chats")]
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

    [Authorize(Policy = "user")]
    [HttpGet("chats", Name = "GetChats")]
    [ProducesResponseType(typeof(GetChatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetChats([FromQuery][BindRequired] GetChatsRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
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
