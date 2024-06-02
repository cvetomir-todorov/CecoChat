using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Testing;

public static class Auth
{
    public static string CreateUserAccessToken(long userId, string userName, JwtOptions jwtOptions)
    {
        byte[] secret = Encoding.UTF8.GetBytes(jwtOptions.Secret);
        SigningCredentials signingCredentials = new(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);

        JwtSecurityTokenHandler jwtTokenHandler = new();
        jwtTokenHandler.OutboundClaimTypeMap.Clear();

        Guid clientId = Guid.NewGuid();
        Claim[] claims =
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Actor, clientId.ToString()),
            new(ClaimTypes.Role, "user")
        };

        DateTime expiration = DateTime.UtcNow.Add(jwtOptions.AccessTokenExpiration);
        JwtSecurityToken jwtToken = new(jwtOptions.Issuer, jwtOptions.Audience, claims, null, expiration, signingCredentials);
        string accessToken = jwtTokenHandler.WriteToken(jwtToken);

        return accessToken;
    }
}
