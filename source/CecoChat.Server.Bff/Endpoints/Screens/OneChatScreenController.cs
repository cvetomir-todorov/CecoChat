using AutoMapper;
using CecoChat.Client.History;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Contracts.Bff.Screens;
using CecoChat.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints.Screens;

[ApiController]
[Route("api/screen/oneChat")]
[ApiExplorerSettings(GroupName = "Screens")]
public class OneChatScreenController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IContractMapper _contractMapper;
    private readonly IHistoryClient _historyClient;
    private readonly IProfileClient _profileClient;

    public OneChatScreenController(
        ILogger<OneChatScreenController> logger,
        IMapper mapper,
        IContractMapper contractMapper,
        IHistoryClient historyClient,
        IProfileClient profileClient)
    {
        _logger = logger;
        _mapper = mapper;
        _contractMapper = contractMapper;
        _historyClient = historyClient;
        _profileClient = profileClient;
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

        IReadOnlyCollection<Contracts.History.HistoryMessage> serviceMessages = await _historyClient.GetHistory(userClaims.UserId, request.OtherUserId, request.MessagesOlderThan, accessToken, ct);
        HistoryMessage[] messages = serviceMessages.Select(message => _contractMapper.MapMessage(message)).ToArray();

        ProfilePublic? profile = null;
        if (request.IncludeProfile)
        {
            Contracts.User.ProfilePublic serviceProfile = await _profileClient.GetPublicProfile(userClaims.UserId, request.OtherUserId, accessToken, ct);
            profile = _mapper.Map<ProfilePublic>(serviceProfile);
        }

        _logger.LogTrace("Responding with {MessageCount} message(s) older than {OlderThan} for chat between {UserId} and {OtherUserId} and (if requested) the profile of the other user {OtherUserId}",
            messages.Length, request.MessagesOlderThan, userClaims.UserId, request.OtherUserId, request.OtherUserId);
        return Ok(new GetOneChatScreenResponse
        {
            Messages = messages,
            Profile = profile
        });
    }
}
