using CecoChat.Bff.Contracts.Auth;
using CecoChat.Bff.Contracts.Chats;
using CecoChat.Bff.Contracts.Connections;
using CecoChat.Bff.Contracts.Files;
using CecoChat.Bff.Contracts.Profiles;
using CecoChat.Bff.Contracts.Screens;
using Refit;

namespace CecoChat.Bff.Contracts;

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

    [Get("/api/chats/user")]
    Task<GetUserChatsResponse> GetUserChats(
        [Query] GetUserChatsRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/chats/history")]
    Task<GetChatHistoryResponse> GetChatHistory(
        [Query] GetChatHistoryRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/profiles/{id}")]
    Task<GetPublicProfileResponse> GetPublicProfile(
        [AliasAs("id")] long userId,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/profiles")]
    Task<GetPublicProfilesResponse> GetPublicProfiles(
        [Query(CollectionFormat.Csv)][AliasAs("userIds")] long[] userIds,
        [Query][AliasAs("searchPattern")] string? searchPattern,
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

    [Get("/api/files/list")]
    Task<GetUserFilesResponse> GetUserFiles(
        [Query] GetUserFilesRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);

    public const string HeaderUploadedFileSize = "Uploaded-File-Size";
    public const string HeaderUploadedFileAllowedUserId = "Uploaded-File-Allowed-UserId";

    [Post("/api/files")]
    [Multipart(boundaryText: "----UserFileBoundary")]
    Task<IApiResponse<UploadFileResponse>> UploadFile(
        [Header(HeaderUploadedFileSize)] long fileSize,
        [Header(HeaderUploadedFileAllowedUserId)] long allowedUser,
        [AliasAs("file")] StreamPart stream,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Get("/api/files/{bucket}/{path}")]
    Task<IApiResponse<HttpContent>> DownloadFile(
        [AliasAs("bucket")] string bucket,
        [AliasAs("path")] string path,
        [Authorize(AuthorizationScheme)] string accessToken);

    [Put("/api/files/{bucket}/{path}/access")]
    Task<IApiResponse<AddFileAccessResponse>> AddFileAccess(
        [AliasAs("bucket")] string bucket,
        [AliasAs("path")] string path,
        [Body] AddFileAccessRequest request,
        [Authorize(AuthorizationScheme)] string accessToken);
}
