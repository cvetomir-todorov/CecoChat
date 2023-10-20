using CecoChat.Contracts.Bff.Auth;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Bff.Connections;
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

    [Put("/api/profile/password")]
    Task<IApiResponse<ChangePasswordResponse>> ChangePassword(
        [Body] ChangePasswordRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Put("/api/profile")]
    Task<IApiResponse<EditProfileResponse>> EditProfile(
        [Body] EditProfileRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/screens/allChats")]
    Task<GetAllChatsScreenResponse> GetAllChatsScreen(
        [Query] GetAllChatsScreenRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/screens/oneChat")]
    Task<GetOneChatScreenResponse> GetOneChatScreen(
        [Query] GetOneChatScreenRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/chats/state")]
    Task<GetChatsResponse> GetStateChats(
        [Query] GetChatsRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/chats/history")]
    Task<GetHistoryResponse> GetHistoryMessages(
        [Query] GetHistoryRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/profiles/{id}")]
    Task<GetPublicProfileResponse> GetPublicProfile(
        [AliasAs("id")] long userId,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/profiles")]
    Task<GetPublicProfilesResponse> GetPublicProfiles(
        [Query(CollectionFormat.Csv)][AliasAs("userIds")] long[] userIds,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Post("/api/connections/{id}/invite")]
    Task<IApiResponse<InviteConnectionResponse>> InviteConnection(
        [AliasAs("id")] long connectionId,
        [Body] InviteConnectionRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Put("/api/connections/{id}/invite")]
    Task<IApiResponse<ApproveConnectionResponse>> ApproveConnection(
        [AliasAs("id")] long connectionId,
        [Body] ApproveConnectionRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Delete("/api/connections/{id}/invite")]
    Task<IApiResponse<CancelConnectionResponse>> CancelConnection(
        [AliasAs("id")] long connectionId,
        [Body] CancelConnectionRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Delete("/api/connections/{id}")]
    Task<IApiResponse<RemoveConnectionResponse>> RemoveConnection(
        [AliasAs("id")] long connectionId,
        [Body] RemoveConnectionRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);
}
