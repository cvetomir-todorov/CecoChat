using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace CecoChat.Server.Identity;

public static class SecurityExtensions
{
    public static bool TryGetBearerAccessTokenValue(this HttpContext context, [NotNullWhen(true)] out string? accessToken)
    {
        accessToken = null;
        if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues values))
        {
            return false;
        }
        if (values.Count > 1)
        {
            return false;
        }

        string value = values.First();
        const string bearerPrefix = "Bearer ";
        if (!value.StartsWith(bearerPrefix, StringComparison.CurrentCultureIgnoreCase))
        {
            return false;
        }

        accessToken = value.Substring(startIndex: bearerPrefix.Length);
        return true;
    }

    public static bool TryGetUserClaims(this HttpContext context, ILogger logger, [NotNullWhen(true)] out UserClaims? userClaims, bool setUserIdTag = true, string userIdTagName = "user.id")
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            logger.LogError("Client from {ClientIp}:{ClientPort} was authorized but has no parseable access token", context.Connection.RemoteIpAddress, context.Connection.RemotePort);
            return false;
        }

        if (setUserIdTag)
        {
            Activity.Current?.SetTag(userIdTagName, userClaims.UserId);
        }

        return true;
    }
}
