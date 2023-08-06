using Refit;

namespace CecoChat.Contracts.Bff;

public interface IBffClient : IDisposable
{
    private const string AuthorizationScheme = "Bearer";

    [Post("/api/registration")]
    Task<IApiResponse> Register(
        [Body] RegisterRequest request);

    [Post("/api/session")]
    Task<IApiResponse<CreateSessionResponse>> CreateSession(
        [Body] CreateSessionRequest request);

    [Get("/api/screen/allChats")]
    Task<GetAllChatsScreenResponse> GetAllChatsScreen(
        [Query] GetAllChatsScreenRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/screen/oneChat")]
    Task<GetOneChatScreenResponse> GetOneChatScreen(
        [Query] GetOneChatScreenRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

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
