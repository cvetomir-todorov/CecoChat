using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CecoChat.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Server.Bff.Endpoints;

public interface IAuthenticator
{
    AuthenticateResult Authenticate(string username, string password);
}

public readonly struct AuthenticateResult
{
    public bool IsAuthenticated { get; init; }
    public long UserId { get; init; }
    public Guid ClientId { get; init; }
    public string AccessToken { get; init; }
}

public class Authenticator : IAuthenticator
{
    private readonly ILogger _logger;
    private readonly JwtOptions _jwtOptions;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _jwtTokenHandler;
    private readonly Dictionary<string, long> _userIdMap;

    public Authenticator(
        ILogger<Authenticator> logger,
        IOptions<AuthOptions> authOptions,
        IOptions<JwtOptions> jwtOptions,
        IClock clock)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
        _clock = clock;

        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);
        _jwtTokenHandler = new();
        _jwtTokenHandler.OutboundClaimTypeMap.Clear();

        _userIdMap = InitUserIdMap(authOptions.Value);
    }

    private Dictionary<string, long> InitUserIdMap(AuthOptions authOptions)
    {
        if (authOptions.ConsoleClientUsers)
        {
            _logger.LogInformation("Using console client users.");

            return new Dictionary<string, long>
            {
                { "bob", 1 },
                { "alice", 2 },
                { "john", 3 },
                { "peter", 1200 }
            };
        }
        else if (authOptions.LoadTestingUsers && authOptions.LoadTestingUserCount > 0)
        {
            _logger.LogInformation("Using load testing users.");

            Dictionary<string, long> map = new();
            for (int i = 0; i < authOptions.LoadTestingUserCount; ++i)
            {
                map.Add($"user{i}", i);
            }

            return map;
        }
        else
        {
            throw new InvalidOperationException("Authentication configuration is invalid.");
        }
    }

    public AuthenticateResult Authenticate(string username, string password)
    {
        if (!_userIdMap.TryGetValue(username, out long userId))
        {
            return new AuthenticateResult {IsAuthenticated = false};
        }

        (Guid clientId, string accessToken) = CreateSession(userId);
        return new AuthenticateResult
        {
            IsAuthenticated = true,
            UserId = userId,
            ClientId = clientId,
            AccessToken = accessToken
        };
    }

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
