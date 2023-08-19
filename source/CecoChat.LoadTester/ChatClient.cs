using CecoChat.Client.Messaging;
using CecoChat.Contracts.Bff;
using CecoChat.Contracts.Bff.Auth;
using CecoChat.Contracts.Bff.Chats;
using CecoChat.Contracts.Messaging;
using Refit;

namespace CecoChat.LoadTester;

public sealed class ChatClient : IAsyncDisposable
{
    private readonly IBffClient _bffClient;
    private readonly IMessagingClient _messagingClient;
    private DateTime _lastGetChats;
    private string? _accessToken;

    public ChatClient(string bffAddress)
    {
        _bffClient = RestService.For<IBffClient>(bffAddress);
        _messagingClient = new MessagingClient();
        _lastGetChats = Snowflake.Epoch;
    }

    public ValueTask DisposeAsync()
    {
        _bffClient.Dispose();
        return _messagingClient.DisposeAsync();
    }

    public int MessagesSent { get; private set; }

    public int MessagesProcessed { get; private set; }

    public int MessagesReceived { get; private set; }

    public async Task Connect(string username, string password, CancellationToken ct)
    {
        CreateSessionRequest request = new()
        {
            UserName = username,
            Password = password
        };

        IApiResponse<CreateSessionResponse> apiResponse = await _bffClient.CreateSession(request);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            throw new InvalidOperationException("Unexpected API error.", apiResponse.Error);
        }

        _messagingClient.MessageDelivered += (_, _) => MessagesProcessed++;
        _messagingClient.MessageReceived += (_, _) => MessagesReceived++;

        CreateSessionResponse response = apiResponse.Content;
        await _messagingClient.Connect(response.MessagingServerAddress, response.AccessToken, ct);

        _accessToken = response.AccessToken;
    }

    public async Task OpenAllChats(long[] userIds)
    {
        GetChatsRequest request = new()
        {
            NewerThan = _lastGetChats
        };
        await _bffClient.GetStateChats(request, _accessToken!);
        _lastGetChats = DateTime.UtcNow;

        if (userIds.Length > 0)
        {
            await _bffClient.GetPublicProfiles(userIds, _accessToken!);
        }
    }

    public async Task OpenOneChat(long userId)
    {
        await _bffClient.GetPublicProfile(userId, _accessToken!);

        GetHistoryRequest request = new()
        {
            OtherUserId = userId,
            OlderThan = DateTime.UtcNow
        };
        await _bffClient.GetHistoryMessages(request, _accessToken!);
    }

    public async Task SendPlainTextMessage(long receiverId, string text)
    {
        SendMessageRequest request = new()
        {
            ReceiverId = receiverId,
            DataType = Contracts.Messaging.DataType.PlainText,
            Data = text
        };
        await _messagingClient.SendMessage(request);
        MessagesSent++;
    }

    public async Task GetHistory(long userId, DateTime olderThan)
    {
        GetHistoryRequest request = new()
        {
            OtherUserId = userId,
            OlderThan = olderThan
        };
        await _bffClient.GetHistoryMessages(request, _accessToken!);
    }
}
