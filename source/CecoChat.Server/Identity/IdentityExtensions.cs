using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace CecoChat.Server.Identity;

public static class IdentityExtensions
{
    private const string UserIdTagName = "cecochat.user_id";

    public static bool TryGetUserClaimsAndAccessToken(
        this HttpContext context,
        ILogger logger,
        [NotNullWhen(true)] out UserClaims? userClaims,
        [NotNullWhen(true)] out string? accessToken,
        bool setUserIdTag = true)
    {
        if (!context.User.TryGetUserClaims(out userClaims))
        {
            logger.LogError("Client named '{Name}' was authorized but has no parseable access token", context.User.Identity?.Name);
            accessToken = null;
            return false;
        }
        if (!context.TryGetAccessToken(out accessToken))
        {
            return false;
        }

        if (setUserIdTag)
        {
            Activity.Current?.SetTag(UserIdTagName, userClaims.UserId);
        }

        return true;
    }

    private static bool TryGetAccessToken(this HttpContext context, [NotNullWhen(true)] out string? accessToken)
    {
        accessToken = null;
        if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues values))
        {
            return false;
        }
        if (values.Count != 1)
        {
            return false;
        }

        string? value = values[0];
        const string bearerPrefix = "Bearer ";
        if (value == null || !value.StartsWith(bearerPrefix, StringComparison.CurrentCultureIgnoreCase))
        {
            return false;
        }

        accessToken = value.Substring(startIndex: bearerPrefix.Length);
        return true;
    }

    public static UserClaims GetUserClaimsHttp(this HttpContext httpContext)
    {
        if (!httpContext.User.TryGetUserClaims(out UserClaims? userClaims))
        {
            throw new InvalidOperationException("User has authenticated but data cannot be parsed from the access token.");
        }

        return userClaims;
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

    public static UserClaims GetUserClaimsGrpc(this ServerCallContext context, ILogger logger, bool setUserIdTag = true)
    {
        HttpContext httpContext = context.GetHttpContext();

        if (!httpContext.User.TryGetUserClaims(out UserClaims? userClaims))
        {
            logger.LogError("Client named '{Name}' was authorized but has no parseable access token", httpContext.User.Identity?.Name);
            throw new RpcException(new Status(StatusCode.Unauthenticated, string.Empty));
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
