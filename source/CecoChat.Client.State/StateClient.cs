using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.State;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.State;

public interface IStateClient : IDisposable
{
    Task<IReadOnlyCollection<ChatState>> GetChats(long userID, DateTime newerThan, string accessToken, CancellationToken ct);
}

internal sealed class StateClient : IStateClient
{
    private readonly ILogger _logger;
    private readonly StateOptions _options;
    private readonly Contracts.State.State.StateClient _client;

    public StateClient(
        ILogger<StateClient> logger,
        IOptions<StateOptions> options,
        Contracts.State.State.StateClient client)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;

        _logger.LogInformation("State address set to {0}.", _options.Address);
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<IReadOnlyCollection<ChatState>> GetChats(long userID, DateTime newerThan, string accessToken, CancellationToken ct)
    {
        GetChatsRequest request = new()
        {
            NewerThan = newerThan.ToTimestamp()
        };

        Metadata grpcMetadata = new();
        grpcMetadata.Add("Authorization", $"Bearer {accessToken}");
        DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
        GetChatsResponse response = await _client.GetChatsAsync(request, headers: grpcMetadata, deadline, cancellationToken: ct);

        _logger.LogTrace("Returned {0} chats for user {1} which are newer than {2}.", response.Chats.Count, userID, newerThan);
        return response.Chats;
    }
}