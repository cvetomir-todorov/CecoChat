using System.Diagnostics;
using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Server.Backplane;
using CecoChat.Server.Bff.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api")]
public class SessionController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IAuthenticator _authenticator;
    private readonly IUserClient _userClient;
    private readonly IPartitionUtility _partitionUtility;
    private readonly IPartitioningConfig _partitioningConfig;

    public SessionController(
        ILogger<SessionController> logger,
        IMapper mapper,
        IAuthenticator authenticator,
        IUserClient userClient,
        IPartitionUtility partitionUtility,
        IPartitioningConfig partitioningConfig)
    {
        _logger = logger;
        _mapper = mapper;
        _authenticator = authenticator;
        _userClient = userClient;
        _partitionUtility = partitionUtility;
        _partitioningConfig = partitioningConfig;
    }

    [HttpPost("session", Name = "Session")]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSession([FromBody][BindRequired] CreateSessionRequest request, CancellationToken ct)
    {
        AuthenticateResult authResult = _authenticator.Authenticate(request.Username, request.Password);
        if (!authResult.IsAuthenticated)
        {
            return Unauthorized();
        }

        Activity.Current?.AddTag("cecochat.user_id", authResult.UserId);
        _logger.LogInformation("User {Username} from {UserIP}:{UserPort} authenticated and assigned user ID {UserId} and client ID {ClientId}",
            request.Username, HttpContext.Connection.RemoteIpAddress, HttpContext.Connection.RemotePort, authResult.UserId, authResult.ClientId);

        Contracts.User.ProfileFull internalProfile = await _userClient.GetFullProfile(authResult.AccessToken, ct);
        ProfileFull profile = _mapper.Map<ProfileFull>(internalProfile);

        int partition = _partitionUtility.ChoosePartition(authResult.UserId, _partitioningConfig.PartitionCount);
        string messagingServerAddress = _partitioningConfig.GetAddress(partition);
        _logger.LogInformation("User {UserId} in partition {Partition} assigned to messaging server {MessagingServer}", authResult.UserId, partition, messagingServerAddress);

        _logger.LogTrace("Responding with new session having client ID {ClientId}, full profile {ProfileUserName} for user {UserId}, using messaging server {MessagingServer}",
            authResult.ClientId, profile.UserName, authResult.UserId, messagingServerAddress);
        CreateSessionResponse response = new()
        {
            ClientId = authResult.ClientId,
            AccessToken = authResult.AccessToken,
            Profile = profile,
            MessagingServerAddress = messagingServerAddress
        };
        return Ok(response);
    }
}
