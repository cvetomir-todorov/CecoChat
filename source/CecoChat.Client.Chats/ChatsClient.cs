using CecoChat.Chats.Contracts;
using Common;
using Common.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.Chats;

public interface IChatsClient : IDisposable
{
    Task<IReadOnlyCollection<ChatState>> GetUserChats(long userId, DateTime newerThan, string accessToken, CancellationToken ct);

    Task<IReadOnlyCollection<HistoryMessage>> GetChatHistory(long userId, long otherUserId, DateTime olderThan, string accessToken, CancellationToken ct);
}

internal sealed class ChatsClient : IChatsClient
{
    private readonly ILogger _logger;
    private readonly ChatsClientOptions _options;
    private readonly CecoChat.Chats.Contracts.Chats.ChatsClient _client;
    private readonly IClock _clock;

    public ChatsClient(
        ILogger<ChatsClient> logger,
        IOptions<ChatsClientOptions> options,
        CecoChat.Chats.Contracts.Chats.ChatsClient client,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _clock = clock;

        _logger.LogInformation("Chats address set to {Address}", _options.Address);
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<IReadOnlyCollection<ChatState>> GetUserChats(long userId, DateTime newerThan, string accessToken, CancellationToken ct)
    {
        GetUserChatsRequest request = new()
        {
            NewerThan = newerThan.ToTimestamp()
        };

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetUserChatsResponse response = await _client.GetUserChatsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ChatCount} chats for user {UserId} which are newer than {NewerThan}", response.Chats.Count, userId, newerThan);
        return response.Chats;
    }

    public async Task<IReadOnlyCollection<HistoryMessage>> GetChatHistory(long userId, long otherUserId, DateTime olderThan, string accessToken, CancellationToken ct)
    {
        GetChatHistoryRequest request = new()
        {
            OtherUserId = otherUserId,
            OlderThan = olderThan.ToTimestamp()
        };

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetChatHistoryResponse response = await _client.GetChatHistoryAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {MessageCount} messages for history between {UserId} and {OtherUserId} older than {OlderThan}", response.Messages.Count, userId, otherUserId, olderThan);
        return response.Messages;
    }
}
