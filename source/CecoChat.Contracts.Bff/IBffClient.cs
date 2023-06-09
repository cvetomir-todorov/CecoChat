using Refit;

namespace CecoChat.Contracts.Bff;

public interface IBffClient : IDisposable
{
    private const string AuthorizationScheme = "Bearer";

    [Post("/api/session")]
    Task<CreateSessionResponse> CreateSession(
        [Body] CreateSessionRequest request);

    [Get("/api/state/chats")]
    Task<GetChatsResponse> GetStateChats(
        [Query] GetChatsRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/history/messages")]
    Task<GetHistoryResponse> GetHistoryMessages(
        [Query] GetHistoryRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/user/profile/{id}")]
    Task<GetPublicProfileResponse> GetPublicProfile(
        [AliasAs("id")] long userId,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/user/profile")]
    Task<GetPublicProfilesResponse> GetPublicProfiles(
        [Query(CollectionFormat.Csv)][AliasAs("userIds")] long[] userIds,
        [Authorize(AuthorizationScheme)] string accessToken);
}
