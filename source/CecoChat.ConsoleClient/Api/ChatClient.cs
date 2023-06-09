using System.IdentityModel.Tokens.Jwt;
using CecoChat.Client.Messaging;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Messaging;
using Refit;

namespace CecoChat.ConsoleClient.Api;

public sealed class AllChatsScreen
{
    public List<LocalStorage.Chat> Chats { get; init; } = new();
    public List<LocalStorage.ProfilePublic> Profiles { get; init; } = new();
}

public sealed class ChatClient : IAsyncDisposable
{
    private readonly IBffClient _bffClient;
    private readonly IMessagingClient _messagingClient;
    private long _userId;
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

    public long UserId => _userId;

    public ProfileFull? UserProfile => _userProfile;

    public async Task CreateSession(string username, string password)
    {
        CreateSessionRequest request = new()
        {
            Username = username,
            Password = password
        };
        CreateSessionResponse response = await _bffClient.CreateSession(request);
        ProcessAccessToken(response.AccessToken);
        _userProfile = response.Profile;
        _messagingServerAddress = response.MessagingServerAddress;
    }

    private void ProcessAccessToken(string accessToken)
    {
        _accessToken = accessToken;

        JwtSecurityToken jwt = new(accessToken);
        _userId = long.Parse(jwt.Subject);
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
        _messagingClient.Disconnected += (sender, e) => Disconnected?.Invoke(sender, e);

        await _messagingClient.Connect(_messagingServerAddress, _accessToken, ct);
    }

    public event EventHandler<ListenNotification>? MessageReceived;

    public event EventHandler<ListenNotification>? ReactionReceived;

    public event EventHandler<ListenNotification>? MessageDelivered;

    public event EventHandler? Disconnected;

    public async Task<IList<LocalStorage.Chat>> GetChats(DateTime newerThan)
    {
        GetChatsRequest request = new()
        {
            NewerThan = newerThan
        };
        GetChatsResponse response = await _bffClient.GetStateChats(request, _accessToken!);
        List<LocalStorage.Chat> chats = Map.BffChats(response.Chats, UserId);

        return chats;
    }

    public async Task<IList<LocalStorage.Message>> GetHistory(long otherUserId, DateTime olderThan)
    {
        GetHistoryRequest request = new()
        {
            OtherUserId = otherUserId,
            OlderThan = olderThan
        };
        GetHistoryResponse response = await _bffClient.GetHistoryMessages(request, _accessToken!);

        List<LocalStorage.Message> messages = new(response.Messages.Length);
        foreach (HistoryMessage bffMessage in response.Messages)
        {
            LocalStorage.Message message = Map.BffMessage(bffMessage);
            messages.Add(message);
        }

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

    public async Task<AllChatsScreen> LoadAllChatsScreen(DateTime chatsNewerThan, bool includeProfiles)
    {
        GetAllChatsScreenRequest request = new()
        {
            ChatsNewerThan = chatsNewerThan,
            IncludeProfiles = includeProfiles
        };
        GetAllChatsScreenResponse response = await _bffClient.GetAllChatsScreen(request, _accessToken!);

        List<LocalStorage.Chat> chats = Map.BffChats(response.Chats, UserId);
        List<LocalStorage.ProfilePublic> profiles = Map.PublicProfiles(response.Profiles);

        return new AllChatsScreen
        {
            Chats = chats,
            Profiles = profiles
        };
    }
}
