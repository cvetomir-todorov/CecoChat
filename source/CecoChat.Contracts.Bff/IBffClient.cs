using CecoChat.Contracts.Bff.Auth;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Contracts.Bff.Screens;
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

    [Post("/api/user/password")]
    Task<IApiResponse<ChangePasswordResponse>> ChangePassword(
        [Body] ChangePasswordRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Post("/api/user")]
    Task<IApiResponse<EditProfileResponse>> EditProfile(
        [Body] EditProfileRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

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
