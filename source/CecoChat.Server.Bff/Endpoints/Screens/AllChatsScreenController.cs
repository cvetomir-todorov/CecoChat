using AutoMapper;
using CecoChat.Client.State;
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
[Route("api/screens/allChats")]
[ApiExplorerSettings(GroupName = "Screens")]
public class AllChatsScreenController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IContractMapper _contractMapper;
    private readonly IStateClient _stateClient;
    private readonly IProfileClient _profileClient;

    public AllChatsScreenController(
        ILogger<AllChatsScreenController> logger,
        IMapper mapper,
        IContractMapper contractMapper,
        IStateClient stateClient,
        IProfileClient profileClient)
    {
        _logger = logger;
        _mapper = mapper;
        _contractMapper = contractMapper;
        _stateClient = stateClient;
        _profileClient = profileClient;
    }

    [Authorize(Policy = "user")]
    [HttpGet(Name = "GetAllChatsScreen")]
    [ProducesResponseType(typeof(GetAllChatsScreenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllChatsScreen([FromQuery][BindRequired] GetAllChatsScreenRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetUserClaimsAndAccessToken(_logger, out UserClaims? userClaims, out string? accessToken))
        {
            return Unauthorized();
        }

        IReadOnlyCollection<Contracts.State.ChatState> serviceChats = await _stateClient.GetChats(userClaims.UserId, request.ChatsNewerThan, accessToken, ct);
        IEnumerable<Contracts.User.ProfilePublic> serviceProfiles = Array.Empty<Contracts.User.ProfilePublic>();
        if (request.IncludeProfiles && serviceChats.Count > 0)
        {
            long[] userIds = serviceChats.Select(chat => chat.OtherUserId).ToArray();
            serviceProfiles = await _profileClient.GetPublicProfiles(userClaims.UserId, userIds, accessToken, ct);
        }

        ChatState[] chats = serviceChats.Select(chat => _contractMapper.MapChat(chat)).ToArray();
        ProfilePublic[] profiles = Array.Empty<ProfilePublic>();
        if (request.IncludeProfiles && chats.Length > 0)
        {
            profiles = serviceProfiles.Select(profile => _mapper.Map<ProfilePublic>(profile)).ToArray();
        }

        _logger.LogTrace("Responding with {ChatCount} chats newer than {NewerThan} and (if requested) their respective user profiles, for all-chats-screen requested by user {UserId}",
            chats.Length, request.ChatsNewerThan, userClaims.UserId);
        return Ok(new GetAllChatsScreenResponse
        {
            Chats = chats,
            Profiles = profiles
        });
    }
}
