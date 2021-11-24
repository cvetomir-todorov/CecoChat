using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.History;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.History
{
    public interface IHistoryClient : IDisposable
    {
        Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userID, long otherUserID, DateTime olderThan, string accessToken, CancellationToken ct);
    }

    internal sealed class HistoryClient : IHistoryClient
    {
        private readonly ILogger _logger;
        private readonly HistoryOptions _options;
        private readonly Contracts.History.History.HistoryClient _client;

        public HistoryClient(
            ILogger<HistoryClient> logger,
            IOptions<HistoryOptions> options,
            Contracts.History.History.HistoryClient client)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;

            _logger.LogInformation("History address set to {0}.", _options.Address);
        }

        public void Dispose()
        {}

        public async Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userID, long otherUserID, DateTime olderThan, string accessToken, CancellationToken ct)
        {
            GetHistoryRequest request = new()
            {
                OtherUserId = otherUserID,
                OlderThan = olderThan.ToTimestamp()
            };

            Metadata grpcMetadata = new();
            grpcMetadata.Add("Authorization", $"Bearer {accessToken}");
            DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);
            GetHistoryResponse response = await _client.GetHistoryAsync(request, headers: grpcMetadata, deadline, cancellationToken: ct);

            _logger.LogTrace("Returned {0} messages for history between {1} and {2} older than {3}.", response.Messages.Count, userID, otherUserID, olderThan);
            return response.Messages;
        }
    }
}