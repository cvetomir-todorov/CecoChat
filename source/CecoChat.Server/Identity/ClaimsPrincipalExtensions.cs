using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.Identity;

public static class ClaimsPrincipalExtensions
{
    private const string UserIdTagName = "cecochat.user_id";

    public static bool TryGetUserClaims(this HttpContext context, ILogger logger, [NotNullWhen(true)] out UserClaims? userClaims, bool setUserIdTag = true)
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            logger.LogError("Client named '{Name}' from {ClientIp}:{ClientPort} was authorized but has no parseable access token",
                context.User.Identity?.Name, context.Connection.RemoteIpAddress, context.Connection.RemotePort);
            return false;
        }

        if (setUserIdTag)
        {
            Activity.Current?.SetTag(UserIdTagName, userClaims.UserId);
        }

        return true;
    }

    public static UserClaims GetUserClaimsSignalR(this ClaimsPrincipal user, string connection, ILogger logger, bool setUserIdTag = true)
    {
        if (!user.TryGetUserClaims(out UserClaims? userClaims))
        {
            logger.LogError("Client named '{Name}' with connection {Connection} was authorized but has no parseable access token", user.Identity?.Name, connection);
            throw new HubException("User has authenticated but data cannot be parsed from the access token.");
        }

        if (setUserIdTag)
        {
            Activity.Current?.SetTag(UserIdTagName, userClaims.UserId);
        }

        return userClaims;
    }

    private static bool TryGetUserClaims(this ClaimsPrincipal user, [NotNullWhen(true)] out UserClaims? userClaims)
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

    private static bool TryGetUserId(this ClaimsPrincipal user, out long userId)
    {
        string? subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
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

    private static bool TryGetClientId(this ClaimsPrincipal user, out Guid clientId)
    {
        string? actor = user.FindFirstValue(ClaimTypes.Actor);
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
