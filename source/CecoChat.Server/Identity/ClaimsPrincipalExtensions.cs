using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CecoChat.Server.Identity;

public sealed class UserClaims
{
    public long UserId { get; set; }

    public Guid ClientId { get; set; }

    public override string ToString()
    {
        return $"{nameof(UserId)}:{UserId} {nameof(ClientId)}:{ClientId}";
    }
}

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserClaims(this ClaimsPrincipal user, [NotNullWhen(true)] out UserClaims? userClaims)
    {
        if (!user.TryGetUserId(out long userId))
        {
            userClaims = default;
            return false;
        }

        if (!user.TryGetClientId(out Guid clientId))
        {
            userClaims = default;
            return false;
        }

        userClaims = new()
        {
            UserId = userId,
            ClientId = clientId
        };
        return true;
    }

    public static bool TryGetUserId(this ClaimsPrincipal user, out long userId)
    {
        string subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(subject))
        {
            userId = default;
            return false;
        }

        if (!long.TryParse(subject, out userId))
        {
            return false;
        }

        return true;
    }

    public static bool TryGetClientId(this ClaimsPrincipal user, out Guid clientId)
    {
        string actor = user.FindFirstValue(ClaimTypes.Actor);
        if (string.IsNullOrWhiteSpace(actor))
        {
            clientId = Guid.Empty;
            return false;
        }

        if (!Guid.TryParse(actor, out clientId))
        {
            return false;
        }

        return true;
    }
}