using System.IdentityModel.Tokens.Jwt;
using CecoChat.Client.Messaging;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Messaging;
using CecoChat.Data;
using Refit;

namespace CecoChat.ConsoleClient.Api;

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

        List<LocalStorage.Chat> chats = new(capacity: response.Chats.Length);
        foreach (ChatState bffChat in response.Chats)
        {
            long otherUserId = DataUtility.GetOtherUsedId(bffChat.ChatID, UserId);
            LocalStorage.Chat chat = Map.BffChat(bffChat, otherUserId);
            chats.Add(chat);
        }

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
        List<LocalStorage.ProfilePublic> profiles = new(capacity: response.Profiles.Length);

        foreach (ProfilePublic profile in response.Profiles)
        {
            profiles.Add(Map.PublicProfile(profile));
        }

        return profiles;
    }
}
