using AutoMapper;
using CecoChat.Client.History;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Bff.Connections;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Contracts.Bff.Screens;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Screens;

[ApiController]
[Route("api/screens/oneChat")]
[ApiExplorerSettings(GroupName = "Screens")]
public class OneChatScreenController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IContractMapper _contractMapper;
    private readonly IHistoryClient _historyClient;
    private readonly IProfileClient _profileClient;
    private readonly IConnectionClient _connectionClient;

    public OneChatScreenController(
        ILogger<OneChatScreenController> logger,
        IMapper mapper,
        IContractMapper contractMapper,
        IHistoryClient historyClient,
        IProfileClient profileClient,
        IConnectionClient connectionClient)
    {
        _logger = logger;
        _mapper = mapper;
        _contractMapper = contractMapper;
        _historyClient = historyClient;
        _profileClient = profileClient;
        _connectionClient = connectionClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet(Name = "GetOneChatScreen")]
    [ProducesResponseType(typeof(GetOneChatScreenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOneChatScreen([FromQuery][BindRequired] GetOneChatScreenRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        // TODO: get data in parallel

        IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages = await _historyClient.GetHistory(userClaims.UserId, request.OtherUserId, request.MessagesOlderThan, accessToken, ct);
        HistoryMessage[] messages = serviceMessages.Select(message => _contractMapper.MapMessage(message)).ToArray();

        ProfilePublic? profile = null;
        if (request.IncludeProfile)
        {
            Contracts.User.ProfilePublic serviceProfile = await _profileClient.GetPublicProfile(userClaims.UserId, request.OtherUserId, accessToken, ct);
            profile = _mapper.Map<ProfilePublic>(serviceProfile);
        }

        Connection? connection = null;
        if (request.IncludeProfile)
        {
            Contracts.User.Connection? serviceConnection = await _connectionClient.GetConnection(userClaims.UserId, request.OtherUserId, accessToken, ct);
            if (serviceConnection != null)
            {
                connection = _mapper.Map<Connection>(serviceConnection);
            }
        }

        _logger.LogTrace("Responding with {MessageCount} message(s) older than {OlderThan} for chat between {UserId} and {OtherUserId} and (if requested) the profile of and the connection with the other user {OtherUserId}",
            messages.Length, request.MessagesOlderThan, userClaims.UserId, request.OtherUserId, request.OtherUserId);
        return Ok(new GetOneChatScreenResponse
        {
            Messages = messages,
            Profile = profile,
            Connection = connection
        });
    }
}
