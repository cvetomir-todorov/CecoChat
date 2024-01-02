using CecoChat.Client.Messaging;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Bff.Auth;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Bff.Connections;
using CecoChat.Contracts.Bff.Files;
using CecoChat.Contracts.Bff.Profiles;
using CecoChat.Contracts.Bff.Screens;
using CecoChat.Contracts.Messaging;
using Refit;

namespace CecoChat.ConsoleClient.Api;

public sealed class ChatClient : IAsyncDisposable
{
    private readonly IBffClient _bffClient;
    private readonly IMessagingClient _messagingClient;
    private ProfileFull? _userProfile;
    private string? _accessToken;
    private string? _messagingServerAddress;

    public ChatClient(string bffAddress)
    {
        _bffClient = RestService.For<IBffClient>(bffAddress);
        _messagingClient = new MessagingClient();
    }

    public ValueTask DisposeAsync()
    {
        _bffClient.Dispose();
        return _messagingClient.DisposeAsync();
    }

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
        if (response.Success)
        {
            _accessToken = apiResponse.Content!.AccessToken;
            _userProfile = apiResponse.Content.Profile;
            _messagingServerAddress = apiResponse.Content.MessagingServerAddress;
        }

        return response;
    }

    public async Task StartMessaging(CancellationToken ct)
    {
        if (_messagingServerAddress == null || _accessToken == null)
        {
            throw new InvalidOperationException("Session should be created first.");
        }

        _messagingClient.MessageReceived += (sender, notification) => MessageReceived?.Invoke(sender, notification);
        _messagingClient.ReactionReceived += (sender, notification) => ReactionReceived?.Invoke(sender, notification);
        _messagingClient.MessageDelivered += (sender, notification) => MessageDelivered?.Invoke(sender, notification);
        _messagingClient.ConnectionNotificationReceived += (sender, notification) => ConnectionNotificationReceived?.Invoke(sender, notification);
        _messagingClient.Disconnected += (sender, e) => Disconnected?.Invoke(sender, e);

        await _messagingClient.Connect(_messagingServerAddress, _accessToken, ct);
    }

    public event EventHandler<ListenNotification>? MessageReceived;

    public event EventHandler<ListenNotification>? ReactionReceived;

    public event EventHandler<ListenNotification>? MessageDelivered;

    public event EventHandler<ListenNotification>? ConnectionNotificationReceived;

    public event EventHandler? Disconnected;

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

    public async Task<long> SendPlainTextMessage(long receiverId, string text)
    {
        SendMessageRequest request = new()
        {
            ReceiverId = receiverId,
            DataType = Contracts.Messaging.DataType.PlainText,
            Data = text
        };
        SendMessageResponse response = await _messagingClient.SendMessage(request);

        return response.MessageId;
    }

    public Task React(long messageId, long senderId, long receiverId, string reaction)
    {
        ReactRequest request = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Reaction = reaction
        };
        return _messagingClient.React(request);
    }

    public Task UnReact(long messageId, long senderId, long receiverId)
    {
        UnReactRequest request = new()
        {
            MessageId = messageId,
            SenderId = senderId,
            ReceiverId = receiverId
        };
        return _messagingClient.UnReact(request);
    }

    public async Task<LocalStorage.ProfilePublic> GetPublicProfile(long userId)
    {
        GetPublicProfileResponse response = await _bffClient.GetPublicProfile(userId, _accessToken!);
        LocalStorage.ProfilePublic profile = Map.PublicProfile(response.Profile);

        return profile;
    }

    public async Task<List<LocalStorage.ProfilePublic>> GetPublicProfiles(IEnumerable<long> userIds)
    {
        GetPublicProfilesResponse response = await _bffClient.GetPublicProfiles(userIds.ToArray(), _accessToken!);
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

    public async Task<ClientResponse<UploadFileResponse>> UploadFile(string filePath, string contentType)
    {
        Stream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(filePath);
        StreamPart part = new(file, fileName, contentType, fileName);
        IApiResponse<UploadFileResponse> apiResponse = await _bffClient.UploadFile(file.Length, part, _accessToken!);

        ClientResponse<UploadFileResponse> response = new();
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
