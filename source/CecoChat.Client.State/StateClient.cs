using CecoChat.Contracts.State;
using CecoChat.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.State;

public interface IStateClient : IDisposable
{
    Task<IReadOnlyCollection<ChatState>> GetChats(long userId, DateTime newerThan, string accessToken, CancellationToken ct);
}

internal sealed class StateClient : IStateClient
{
    private readonly ILogger _logger;
    private readonly StateOptions _options;
    private readonly Contracts.State.State.StateClient _client;
    private readonly IClock _clock;

    public StateClient(
        ILogger<StateClient> logger,
        IOptions<StateOptions> options,
        Contracts.State.State.StateClient client,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _clock = clock;

        _logger.LogInformation("State address set to {Address}", _options.Address);
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<IReadOnlyCollection<ChatState>> GetChats(long userId, DateTime newerThan, string accessToken, CancellationToken ct)
    {
        GetChatsRequest request = new()
        {
            NewerThan = newerThan.ToTimestamp()
        };

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetChatsResponse response = await _client.GetChatsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ChatCount} chats for user {UserId} which are newer than {NewerThan}", response.Chats.Count, userId, newerThan);
        return response.Chats;
    }
}
