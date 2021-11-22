using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.IDGen;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.IDGen
{
    public interface IIDGenClient : IDisposable
    {
        ValueTask<GetIDResult> GetID(CancellationToken ct);
    }

    public readonly struct GetIDResult
    {
        public bool Success => ID > 0;
        public long ID { get; init; }
    }

    public sealed class IDGenClient : IIDGenClient
    {
        private readonly ILogger _logger;
        private readonly IDGenOptions _options;
        private readonly Contracts.IDGen.IDGen.IDGenClient _client;
        private readonly ConcurrentQueue<long> _idBuffer;
        private readonly BlockingCollection<long> _idChannel;
        private readonly int _getIDWaitIntervalMilliseconds;
        private readonly Timer _invalidateIDsTimer;

        public IDGenClient(
            ILogger<IDGenClient> logger,
            IOptions<IDGenOptions> options,
            Contracts.IDGen.IDGen.IDGenClient client)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;

            _logger.LogInformation("IDGen address set to {0}.", _options.Communication.Address);
            _idBuffer = new();
            _idChannel = new(_idBuffer);
            _getIDWaitIntervalMilliseconds = (int)_options.Generation.GetIDWaitInterval.TotalMilliseconds;
            _invalidateIDsTimer = new(
                callback: _ => RefreshIDs(),
                state: null,
                dueTime: TimeSpan.Zero, 
                period: _options.Generation.RefreshIDsInterval);
        }

        public void Dispose()
        {
            _invalidateIDsTimer.Dispose();
        }

        public ValueTask<GetIDResult> GetID(CancellationToken ct)
        {
            if (!_idChannel.TryTake(out long id, _getIDWaitIntervalMilliseconds, ct))
            {
                _logger.LogWarning("Timed-out while waiting for new IDs to be generated.");
                return ValueTask.FromResult(new GetIDResult());
            }

            return ValueTask.FromResult(new GetIDResult {ID = id});
        }

        private void RefreshIDs()
        {
            try
            {
                GenerateManyRequest request = new()
                {
                    OriginatorId = _options.Generation.OriginatorID,
                    Count = _options.Generation.RefreshIDsCount
                };
                DateTime deadline = DateTime.UtcNow.Add(_options.Communication.CallTimeout);

                GenerateManyResponse response = _client.GenerateMany(request, deadline: deadline);
                // consider using 2 channels and switch them atomically in a green-blue manner
                // concurrent collections Clear method could be slow due to locking 
                _idBuffer.Clear();
                foreach (long id in response.Ids)
                {
                    _idChannel.Add(id);
                }
            }
            catch (RpcException rpcException)
            {
                _logger.LogError(rpcException, "Failed to refresh IDs due to error {0}.", rpcException.Status);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to refresh IDs.");
            }
        }
    }
}
