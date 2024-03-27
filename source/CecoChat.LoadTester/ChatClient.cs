using CecoChat.Bff.Contracts;
using CecoChat.Bff.Contracts.Auth;
using CecoChat.Bff.Contracts.Chats;
using CecoChat.Messaging.Client;
using Common;
using Refit;

namespace CecoChat.LoadTester;

public sealed class ChatClient : IAsyncDisposable
{
    private readonly IBffClient _bffClient;
    private IMessagingClient? _messagingClient;
    private DateTime _lastGetChats;
    private string? _accessToken;

    public ChatClient(string bffAddress)
    {
        _bffClient = RestService.For<IBffClient>(bffAddress);
        _lastGetChats = Snowflake.Epoch;
    }

    public ValueTask DisposeAsync()
    {
        _bffClient.Dispose();

        if (_messagingClient != null)
        {
            return _messagingClient.DisposeAsync();
        }

        return ValueTask.CompletedTask;
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

        CreateSessionResponse response = apiResponse.Content;
        _messagingClient = new MessagingClient(response.AccessToken, response.MessagingServerAddress);
        _messagingClient.MessageDelivered += (_, _) => MessagesProcessed++;
        _messagingClient.PlainTextReceived += (_, _) => MessagesReceived++;

        await _messagingClient.Connect(ct);

        _accessToken = response.AccessToken;
    }

    public async Task OpenAllChats(long[] userIds)
    {
        GetUserChatsRequest request = new()
        {
            NewerThan = _lastGetChats
        };
        await _bffClient.GetUserChats(request, _accessToken!);
        _lastGetChats = DateTime.UtcNow;

        if (userIds.Length > 0)
        {
            await _bffClient.GetPublicProfiles(userIds, searchPattern: null, _accessToken!);
        }
    }

    public async Task OpenOneChat(long userId)
    {
        await _bffClient.GetPublicProfile(userId, _accessToken!);

        GetChatHistoryRequest request = new()
        {
            OtherUserId = userId,
            OlderThan = DateTime.UtcNow
        };
        await _bffClient.GetChatHistory(request, _accessToken!);
    }

    public async Task SendPlainTextMessage(long receiverId, string text)
    {
        if (_messagingClient == null)
        {
            throw new InvalidOperationException($"Messaging client is not connected. Call {nameof(Connect)} first.");
        }

        await _messagingClient.SendPlainTextMessage(receiverId, text);
        MessagesSent++;
    }

    public async Task GetChatHistory(long userId, DateTime olderThan)
    {
        GetChatHistoryRequest request = new()
        {
            OtherUserId = userId,
            OlderThan = olderThan
        };
        await _bffClient.GetChatHistory(request, _accessToken!);
    }
}
