using CecoChat.Bff.Contracts;
using CecoChat.Bff.Contracts.Auth;
using CecoChat.Bff.Contracts.Chats;
using CecoChat.Bff.Contracts.Connections;
using CecoChat.Bff.Contracts.Files;
using CecoChat.Bff.Contracts.Profiles;
using CecoChat.Bff.Contracts.Screens;
using Refit;

namespace CecoChat.ConsoleClient.Api;

public sealed class ChatClient : IDisposable
{
    private readonly IBffClient _bffClient;
    private ProfileFull? _userProfile;
    private string? _accessToken;
    private string? _messagingServerAddress;

    public ChatClient(string bffAddress)
    {
        _bffClient = RestService.For<IBffClient>(bffAddress);
    }

    public void Dispose()
    {
        _bffClient.Dispose();
    }

    public string AccessToken => _accessToken!;
    public string MessagingServerAddress => _messagingServerAddress!;
    public long UserId => _userProfile!.UserId;
    public ProfileFull? UserProfile => _userProfile;

    public async Task<ClientResponse> Register(string userName, string password, string displayName, string phone, string email)
    {
        RegisterRequest request = new()
        {
            UserName = userName,
            Password = password,
            DisplayName = displayName,
            Phone = phone,
            Email = email
        };
        IApiResponse apiResponse = await _bffClient.Register(request);
        ClientResponse response = new();
        ProcessApiResponse(apiResponse, response);

        return response;
    }

    public async Task<ClientResponse> CreateSession(string username, string password)
    {
        CreateSessionRequest request = new()
        {
            UserName = username,
            Password = password
        };
        IApiResponse<CreateSessionResponse> apiResponse = await _bffClient.CreateSession(request);

        ClientResponse response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success && apiResponse.Content != null)
        {
            _accessToken = apiResponse.Content.AccessToken;
            _messagingServerAddress = apiResponse.Content.MessagingServerAddress;
            _userProfile = apiResponse.Content.Profile;
        }

        return response;
    }

    public async Task<ClientResponse> ChangePassword(string newPassword)
    {
        ChangePasswordRequest request = new()
        {
            NewPassword = newPassword,
            Version = _userProfile!.Version
        };
        IApiResponse<ChangePasswordResponse> apiResponse = await _bffClient.ChangePassword(request, _accessToken!);
        ClientResponse response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            UserProfile!.Version = apiResponse.Content!.NewVersion;
        }

        return response;
    }

    public async Task<ClientResponse> EditProfile(string displayName)
    {
        EditProfileRequest request = new()
        {
            DisplayName = displayName,
            Version = _userProfile!.Version
        };
        IApiResponse<EditProfileResponse> apiResponse = await _bffClient.EditProfile(request, _accessToken!);
        ClientResponse response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            UserProfile!.Version = apiResponse.Content!.NewVersion;
        }

        return response;
    }

    public async Task<IList<LocalStorage.Chat>> GetUserChats(DateTime newerThan)
    {
        GetUserChatsRequest request = new()
        {
            NewerThan = newerThan
        };
        GetUserChatsResponse response = await _bffClient.GetUserChats(request, _accessToken!);
        List<LocalStorage.Chat> chats = Map.BffChats(response.Chats, UserId);

        return chats;
    }

    public async Task<IList<LocalStorage.Message>> GetChatHistory(long otherUserId, DateTime olderThan)
    {
        GetChatHistoryRequest request = new()
        {
            OtherUserId = otherUserId,
            OlderThan = olderThan
        };
        GetChatHistoryResponse response = await _bffClient.GetChatHistory(request, _accessToken!);
        List<LocalStorage.Message> messages = Map.BffMessages(response.Messages);

        return messages;
    }

    public async Task<LocalStorage.ProfilePublic> GetPublicProfile(long userId)
    {
        GetPublicProfileResponse response = await _bffClient.GetPublicProfile(userId, _accessToken!);
        LocalStorage.ProfilePublic profile = Map.PublicProfile(response.Profile);

        return profile;
    }

    public async Task<List<LocalStorage.ProfilePublic>> GetPublicProfiles(IEnumerable<long> userIds)
    {
        GetPublicProfilesResponse response = await _bffClient.GetPublicProfiles(userIds.ToArray(), searchPattern: null, _accessToken!);
        List<LocalStorage.ProfilePublic> profiles = Map.PublicProfiles(response.Profiles);

        return profiles;
    }

    public async Task<List<LocalStorage.ProfilePublic>> GetPublicProfiles(string searchPattern)
    {
        GetPublicProfilesResponse response = await _bffClient.GetPublicProfiles(Array.Empty<long>(), searchPattern, _accessToken!);
        List<LocalStorage.ProfilePublic> profiles = Map.PublicProfiles(response.Profiles);

        return profiles;
    }

    public async Task<AllChatsScreen> LoadAllChatsScreen(DateTime chatsNewerThan, DateTime filesNewerThan, bool includeProfiles)
    {
        GetAllChatsScreenRequest request = new()
        {
            ChatsNewerThan = chatsNewerThan,
            FilesNewerThan = filesNewerThan,
            IncludeProfiles = includeProfiles
        };
        GetAllChatsScreenResponse response = await _bffClient.GetAllChatsScreen(request, _accessToken!);

        if (includeProfiles && response.Profiles.Length < response.Chats.Length)
        {
            throw new InvalidOperationException($"Loading all chats screen, returned {response.Chats.Length} chats but profiles were {response.Profiles.Length}.");
        }

        List<LocalStorage.Chat> chats = Map.BffChats(response.Chats, UserId);
        List<LocalStorage.Connection> connections = Map.Connections(response.Connections);
        List<LocalStorage.ProfilePublic> profiles = Map.PublicProfiles(response.Profiles);
        List<LocalStorage.FileRef> files = Map.Files(response.Files);

        return new AllChatsScreen
        {
            Chats = chats,
            Connections = connections,
            Profiles = profiles,
            Files = files
        };
    }

    public async Task<OneChatScreen> LoadOneChatScreen(long otherUserId, DateTime messagesOlderThan, bool includeProfile, bool includeConnection)
    {
        GetOneChatScreenRequest request = new()
        {
            OtherUserId = otherUserId,
            MessagesOlderThan = messagesOlderThan,
            IncludeProfile = includeProfile,
            IncludeConnection = includeConnection
        };
        GetOneChatScreenResponse response = await _bffClient.GetOneChatScreen(request, _accessToken!);

        List<LocalStorage.Message> messages = Map.BffMessages(response.Messages);
        LocalStorage.ProfilePublic? profile = null;
        if (includeProfile && response.Profile != null)
        {
            profile = Map.PublicProfile(response.Profile);
        }

        LocalStorage.Connection? connection = null;
        if (includeConnection && response.Connection != null)
        {
            connection = Map.Connection(response.Connection);
        }

        return new OneChatScreen
        {
            Messages = messages,
            Profile = profile,
            Connection = connection
        };
    }

    public async Task<ClientResponse<InviteConnectionResponse>> InviteConnection(long connectionId)
    {
        InviteConnectionRequest request = new();
        IApiResponse<InviteConnectionResponse> apiResponse = await _bffClient.InviteConnection(connectionId, request, _accessToken!);

        ClientResponse<InviteConnectionResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    public async Task<ClientResponse<ApproveConnectionResponse>> ApproveConnection(long connectionId, DateTime version)
    {
        ApproveConnectionRequest request = new()
        {
            Version = version
        };
        IApiResponse<ApproveConnectionResponse> apiResponse = await _bffClient.ApproveConnection(connectionId, request, _accessToken!);

        ClientResponse<ApproveConnectionResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    public async Task<ClientResponse<CancelConnectionResponse>> CancelConnection(long connectionId, DateTime version)
    {
        CancelConnectionRequest request = new()
        {
            Version = version
        };
        IApiResponse<CancelConnectionResponse> apiResponse = await _bffClient.CancelConnection(connectionId, request, _accessToken!);

        ClientResponse<CancelConnectionResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    public async Task<ClientResponse<RemoveConnectionResponse>> RemoveConnection(long connectionId, DateTime version)
    {
        RemoveConnectionRequest request = new()
        {
            Version = version
        };
        IApiResponse<RemoveConnectionResponse> apiResponse = await _bffClient.RemoveConnection(connectionId, request, _accessToken!);

        ClientResponse<RemoveConnectionResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    public async Task<List<LocalStorage.FileRef>> GetUserFiles(DateTime newerThan)
    {
        GetUserFilesRequest request = new()
        {
            NewerThan = newerThan
        };
        GetUserFilesResponse response = await _bffClient.GetUserFiles(request, _accessToken!);

        List<LocalStorage.FileRef> files = Map.Files(response.Files);
        return files;
    }

    public async Task<ClientResponse<UploadFileResponse>> UploadFile(Stream fileStream, string fileName, string contentType, long allowedUserId)
    {
        StreamPart part = new(fileStream, fileName, contentType, fileName);
        IApiResponse<UploadFileResponse> apiResponse = await _bffClient.UploadFile(fileStream.Length, allowedUserId, part, _accessToken!);

        ClientResponse<UploadFileResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    public async Task<ClientResponse<Stream>> DownloadFile(string bucket, string path)
    {
        IApiResponse<HttpContent> apiResponse = await _bffClient.DownloadFile(bucket, path, _accessToken!);

        ClientResponse<Stream> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = await apiResponse.Content!.ReadAsStreamAsync();
        }

        return response;
    }

    public async Task<ClientResponse<AddFileAccessResponse>> AddFileAccess(string bucket, string path, DateTime version, long allowedUserId)
    {
        AddFileAccessRequest request = new()
        {
            AllowedUserId = allowedUserId,
            Version = version
        };
        IApiResponse<AddFileAccessResponse> apiResponse = await _bffClient.AddFileAccess(bucket, path, request, _accessToken!);

        ClientResponse<AddFileAccessResponse> response = new();
        ProcessApiResponse(apiResponse, response);

        if (response.Success)
        {
            response.Content = apiResponse.Content;
        }

        return response;
    }

    private static void ProcessApiResponse(IApiResponse apiResponse, ClientResponse response)
    {
        if (apiResponse.IsSuccessStatusCode)
        {
            response.Success = true;
        }
        else if (apiResponse.Error is ValidationApiException validationApiException)
        {
            if (validationApiException.Content != null)
            {
                foreach (KeyValuePair<string, string[]> errorPair in validationApiException.Content.Errors)
                {
                    response.Errors.AddRange(errorPair.Value);
                }
            }

            if (response.Errors.Count == 0)
            {
                response.Errors.Add($"{apiResponse.Error.Message}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(apiResponse.Error.Content))
        {
            response.Errors.Add($"{(int)apiResponse.StatusCode} {apiResponse.StatusCode}: {apiResponse.Error.Content}");
        }
        else
        {
            response.Errors.Add($"{apiResponse.Error.Message}");
        }
    }
}
