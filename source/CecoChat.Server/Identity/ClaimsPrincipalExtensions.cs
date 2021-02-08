using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CecoChat.Server.Identity
{
    public static class ClaimsPrincipalExtensions
    {
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
                userID = default;
                return false;
            }

            return true;
        }
    }
}
