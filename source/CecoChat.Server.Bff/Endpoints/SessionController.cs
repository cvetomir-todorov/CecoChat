using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using CecoChat.Client.User;
using CecoChat.Contracts.Bff;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Jwt;
using CecoChat.Server.Backplane;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Server.Bff.Endpoints;

[ApiController]
[Route("api")]
public class SessionController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly JwtOptions _jwtOptions;
    private readonly IMapper _mapper;
    private readonly IClock _clock;
    private readonly IUserClient _userClient;
    private readonly IPartitionUtility _partitionUtility;
    private readonly IPartitioningConfig _partitioningConfig;

    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _jwtTokenHandler;

    public SessionController(
        ILogger<SessionController> logger,
        IOptions<JwtOptions> jwtOptions,
        IMapper mapper,
        IClock clock,
        IUserClient userClient,
        IPartitionUtility partitionUtility,
        IPartitioningConfig partitioningConfig)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
        _mapper = mapper;
        _clock = clock;
        _userClient = userClient;
        _partitionUtility = partitionUtility;
        _partitioningConfig = partitioningConfig;

        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);
        _jwtTokenHandler = new();
        _jwtTokenHandler.OutboundClaimTypeMap.Clear();
    }

    [HttpPost("session", Name = "Session")]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSession([FromBody][BindRequired] CreateSessionRequest request, CancellationToken ct)
    {
        if (!_userIdMap.TryGetValue(request.Username, out long userId))
        {
            return Unauthorized();
        }
        Activity.Current?.AddTag("user.id", userId);

        (Guid clientId, string accessToken) = CreateSession(userId);
        _logger.LogInformation("User {Username} from {UserIP}:{UserPort} authenticated and assigned user ID {UserId} and client ID {ClientId}",
            request.Username, HttpContext.Connection.RemoteIpAddress, HttpContext.Connection.RemotePort, userId, clientId);

        Contracts.User.ProfileFull internalProfile = await _userClient.GetFullProfile(accessToken, ct);
        ProfileFull profile = _mapper.Map<ProfileFull>(internalProfile);

        int partition = _partitionUtility.ChoosePartition(userId, _partitioningConfig.PartitionCount);
        string messagingServerAddress = _partitioningConfig.GetAddress(partition);
        _logger.LogInformation("User {UserId} in partition {Partition} assigned to messaging server {MessagingServer}", userId, partition, messagingServerAddress);

        _logger.LogTrace("Responding with new session having client ID {ClientId}, full profile {ProfileUserName} for user {UserId}, using messaging server {MessagingServer}",
            clientId, profile.UserName, userId, messagingServerAddress);
        CreateSessionResponse response = new()
        {
            ClientId = clientId,
            AccessToken = accessToken,
            Profile = profile,
            MessagingServerAddress = messagingServerAddress
        };
        return Ok(response);
    }

    private readonly Dictionary<string, long> _userIdMap = new()
    {
        { "bob", 1 },
        { "alice", 2 },
        { "john", 3 },
        { "peter", 1200 }
    };

    private (Guid, string) CreateSession(long userId)
    {
        Guid clientId = Guid.NewGuid();
        Claim[] claims =
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Actor, clientId.ToString()),
            new(ClaimTypes.Role, "user")
        };

        DateTime expiration = _clock.GetNowUtc().Add(_jwtOptions.AccessTokenExpiration);
        JwtSecurityToken jwtToken = new(_jwtOptions.Issuer, _jwtOptions.Audience, claims, null, expiration, _signingCredentials);
        string accessToken = _jwtTokenHandler.WriteToken(jwtToken);

        return (clientId, accessToken);
    }
}