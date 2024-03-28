using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using CecoChat.Bff.Contracts.Auth;
using CecoChat.Config.Sections.Partitioning;
using CecoChat.Server.Backplane;
using CecoChat.User.Client;
using Common;
using Common.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Bff.Service.Endpoints.Auth;

[ApiController]
[Route("api")]
[ApiExplorerSettings(GroupName = "Auth")]
public class SessionController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IAuthClient _authClient;
    private readonly IPartitioner _partitioner;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly JwtOptions _jwtOptions;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _jwtTokenHandler;

    public SessionController(
        ILogger<SessionController> logger,
        IMapper mapper,
        IAuthClient authClient,
        IOptions<JwtOptions> jwtOptions,
        IClock clock,
        IPartitioner partitioner,
        IPartitioningConfig partitioningConfig)
    {
        _logger = logger;
        _mapper = mapper;
        _authClient = authClient;
        _jwtOptions = jwtOptions.Value;
        _clock = clock;
        _partitioner = partitioner;
        _partitioningConfig = partitioningConfig;

        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);
        _jwtTokenHandler = new();
        _jwtTokenHandler.OutboundClaimTypeMap.Clear();
    }

    [AllowAnonymous]
    [HttpPost("session", Name = "Session")]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSession([FromBody][BindRequired] CreateSessionRequest request, CancellationToken ct)
    {
        AuthenticateResult authenticateResult = await _authClient.Authenticate(request.UserName, request.Password, ct);
        if (authenticateResult.Missing || authenticateResult.InvalidPassword)
        {
            _logger.LogTrace("Responding with no session because authentication failed for user {UserName}", request.UserName);
            return Unauthorized();
        }
        if (authenticateResult.Profile == null)
        {
            throw new InvalidOperationException($"Failed to process {nameof(AuthenticateResult)}.");
        }

        (Guid clientId, string accessToken) = CreateSession(authenticateResult.Profile.UserId, authenticateResult.Profile.UserName);
        Activity.Current?.AddTag("cecochat.user_id", authenticateResult.Profile.UserId);
        _logger.LogTrace("User {UserId} named {UserName} authenticated and assigned client ID {ClientId}",
            authenticateResult.Profile.UserId, authenticateResult.Profile.UserName, clientId);

        int partition = _partitioner.ChoosePartition(authenticateResult.Profile.UserId, _partitioningConfig.PartitionCount);
        string messagingServerAddress = _partitioningConfig.GetAddress(partition);
        _logger.LogTrace("User {UserId} named {UserName} assigned to partition {Partition} and messaging server {MessagingServer}",
            authenticateResult.Profile.UserId, authenticateResult.Profile.UserName, partition, messagingServerAddress);

        ProfileFull profile = _mapper.Map<ProfileFull>(authenticateResult.Profile)!;
        _logger.LogTrace("Responding with a new session for user {UserId} named {UserName} with client ID {ClientId}, partition {Partition}, messaging server {MessagingServer}",
            authenticateResult.Profile.UserId, profile.UserName, clientId, partition, messagingServerAddress);
        CreateSessionResponse response = new()
        {
            ClientId = clientId,
            AccessToken = accessToken,
            Profile = profile,
            MessagingServerAddress = messagingServerAddress
        };
        return Ok(response);
    }

    private (Guid, string) CreateSession(long userId, string userName)
    {
        Guid clientId = Guid.NewGuid();
        Claim[] claims =
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Actor, clientId.ToString()),
            new(ClaimTypes.Role, "user")
        };

        DateTime expiration = _clock.GetNowUtc().Add(_jwtOptions.AccessTokenExpiration);
        JwtSecurityToken jwtToken = new(_jwtOptions.Issuer, _jwtOptions.Audience, claims, null, expiration, _signingCredentials);
        string accessToken = _jwtTokenHandler.WriteToken(jwtToken);

        return (clientId, accessToken);
    }
}
