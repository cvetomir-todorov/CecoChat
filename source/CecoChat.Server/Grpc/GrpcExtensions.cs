using System.Diagnostics;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CecoChat.Server.Grpc;

public static class GrpcExtensions
{
    public static UserClaims GetUserClaims(this ServerCallContext context, ILogger logger, bool setUserIdTag = true, string userIdTagName = "user.id")
    {
        if (!context.GetHttpContext().User.TryGetUserClaims(out UserClaims? userClaims))
        {
            logger.LogError("Client from {ClientAddress} was authorized but has no parseable access token", context.Peer);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Access token could not be parsed."));
        }

        if (setUserIdTag)
        {
            Activity.Current?.SetTag(userIdTagName, userClaims.UserId);
        }

        return userClaims;
    }
}