using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CecoChat.Server.Identity;

public sealed class UserClaims
{
    public long UserID { get; set; }

    public Guid ClientID { get; set; }

    public override string ToString()
    {
        return $"{nameof(UserID)}:{UserID} {nameof(ClientID)}:{ClientID}";
    }
}

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserClaims(this ClaimsPrincipal user, out UserClaims userClaims)
    {
        if (!user.TryGetUserID(out long userID))
        {
            userClaims = default;
            return false;
        }

        if (!user.TryGetClientID(out Guid clientID))
        {
            userClaims = default;
            return false;
        }

        userClaims = new()
        {
            UserID = userID,
            ClientID = clientID
        };
        return true;
    }

    public static bool TryGetUserID(this ClaimsPrincipal user, out long userID)
    {
        string subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(subject))
        {
            userID = default;
            return false;
        }

        if (!long.TryParse(subject, out userID))
        {
            return false;
        }

        return true;
    }

    public static bool TryGetClientID(this ClaimsPrincipal user, out Guid clientID)
    {
        string actor = user.FindFirstValue(ClaimTypes.Actor);
        if (string.IsNullOrWhiteSpace(actor))
        {
            clientID = Guid.Empty;
            return false;
        }

        if (!Guid.TryParse(actor, out clientID))
        {
            return false;
        }

        return true;
    }
}