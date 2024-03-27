using AutoMapper;
using CecoChat.Chats.Client;
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
    private readonly IChatsClient _chatsClient;
    private readonly IProfileClient _profileClient;
    private readonly IConnectionClient _connectionClient;

    public OneChatScreenController(
        ILogger<OneChatScreenController> logger,
        IMapper mapper,
        IContractMapper contractMapper,
        IChatsClient chatsClient,
        IProfileClient profileClient,
        IConnectionClient connectionClient)
    {
        _logger = logger;
        _mapper = mapper;
        _contractMapper = contractMapper;
        _chatsClient = chatsClient;
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

        LinkedList<Task> tasks = new();

        Task<IReadOnlyCollection<CecoChat.Chats.Contracts.HistoryMessage>> messagesTask = _chatsClient.GetChatHistory(userClaims.UserId, request.OtherUserId, request.MessagesOlderThan, accessToken, ct);
        tasks.AddLast(messagesTask);

        Task<User.Contracts.ProfilePublic?>? profileTask = null;
        if (request.IncludeProfile)
        {
            profileTask = _profileClient.GetPublicProfile(userClaims.UserId, request.OtherUserId, accessToken, ct);
            tasks.AddLast(profileTask);
        }

        Task<User.Contracts.Connection?>? connectionTask = null;
        if (request.IncludeProfile)
        {
            connectionTask = _connectionClient.GetConnection(userClaims.UserId, request.OtherUserId, accessToken, ct);
            tasks.AddLast(connectionTask);
        }

        await Task.WhenAll(tasks);

        IReadOnlyCollection<CecoChat.Chats.Contracts.HistoryMessage> serviceMessages = messagesTask.Result;
        HistoryMessage[] messages = serviceMessages.Select(message => _contractMapper.MapMessage(message)).ToArray();

        ProfilePublic? profile = null;
        if (request.IncludeProfile)
        {
            User.Contracts.ProfilePublic? serviceProfile = profileTask!.Result;
            if (serviceProfile != null)
            {
                profile = _mapper.Map<ProfilePublic>(serviceProfile);
            }
        }

        Connection? connection = null;
        if (request.IncludeConnection)
        {
            User.Contracts.Connection? serviceConnection = connectionTask!.Result;
            if (serviceConnection != null)
            {
                connection = _mapper.Map<Connection>(serviceConnection);
            }
        }

        _logger.LogTrace("Responding with {MessageCount} message(s) older than {OlderThan} for chat between {UserId} and {OtherUserId} and, if requested and existing - the profile of and the connection with the other user",
            messages.Length, request.MessagesOlderThan, userClaims.UserId, request.OtherUserId);
        return Ok(new GetOneChatScreenResponse
        {
            Messages = messages,
            Profile = profile,
            Connection = connection
        });
    }
}
