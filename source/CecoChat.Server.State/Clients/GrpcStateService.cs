using System.Diagnostics;
using CecoChat.Contracts.State;
using CecoChat.Data.State.Repos;
using CecoChat.Server.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace CecoChat.Server.State.Clients;

public class GrpcStateService : Contracts.State.State.StateBase
{
    private readonly ILogger _logger;
    private readonly IChatStateRepo _repo;

    public GrpcStateService(
        ILogger<GrpcStateService> logger,
        IChatStateRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    [Authorize(Roles = "user")]
    public override async Task<GetChatsResponse> GetChats(GetChatsRequest request, ServerCallContext context)
    {
        long userId = GetUserId(context);
        DateTime newerThan = request.NewerThan.ToDateTime();
        IReadOnlyCollection<ChatState> chats = await _repo.GetChats(userId, newerThan);

        GetChatsResponse response = new();
        response.Chats.Add(chats);

        _logger.LogTrace("Responding with {ChatCount} chats for user {UserId}", chats.Count, userId);
        return response;
    }

    private static long GetUserId(ServerCallContext context)
    {
        if (!context.GetHttpContext().User.TryGetUserID(out long userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Client has no parseable access token."));
        }
        Activity.Current?.SetTag("user.id", userId);
        return userId;
    }
}
