using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.IDGen;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.IDGen
{
    public interface IIDGenClient : IDisposable
    {
        ValueTask<GetIDResult> GetID(long userID, CancellationToken ct);
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
        private readonly SemaphoreSlim _generatedNewIDs;
        private readonly Timer _invalidateIDsTimer;
        private int _isGeneratingNewIDs;
        private const int True = 1;
        private const int False = 0;

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
            _generatedNewIDs = new(initialCount: 0, maxCount: _options.Generation.MaxConcurrentRequests);
            _invalidateIDsTimer = new(
                callback: InvalidateIDs, state: null,
                dueTime: _options.Generation.InvalidateIDsInterval,
                period: _options.Generation.InvalidateIDsInterval);
        }

        public void Dispose()
        {
            _generatedNewIDs.Dispose();
            _invalidateIDsTimer.Dispose();
        }

        public async ValueTask<GetIDResult> GetID(long userID, CancellationToken ct)
        {
            if (_idBuffer.TryDequeue(out long id))
            {
                return new GetIDResult {ID = id};
            }

            TriggerNewIDGeneration(userID, _options.Generation.RefreshIDsCount);
            if (!await _generatedNewIDs.WaitAsync(_options.Generation.GetIDWaitInterval, ct))
            {
                _logger.LogWarning("Timed-out while waiting for new IDs to be generated.");
                return new GetIDResult();
            }

            if (!_idBuffer.TryDequeue(out id))
            {
                _logger.LogWarning("No ID available after ensuring generating new IDs is triggered.");
                return new GetIDResult();
            }

            return new GetIDResult {ID = id};
        }

        private void TriggerNewIDGeneration(long originatorID, int count)
        {
            // atomically ensure that we are the only thread that will trigger ID generation
            if (True == Interlocked.CompareExchange(ref _isGeneratingNewIDs, value: True, comparand: False))
            {
                return;
            }

            Task _ = GenerateNewIDsAsync(originatorID, count);
        }

        private async Task GenerateNewIDsAsync(long originatorID, int count)
        {
            try
            {
                GenerateManyRequest request = new()
                {
                    OriginatorId = originatorID,
                    Count = count
                };
                DateTime deadline = DateTime.UtcNow.Add(_options.Communication.CallTimeout);

                GenerateManyResponse response = await _client.GenerateManyAsync(request, deadline: deadline);
                foreach (long id in response.Ids)
                {
                    _idBuffer.Enqueue(id);
                }
            }
            catch (RpcException rpcException)
            {
                _logger.LogError(rpcException, "Failed to generate new IDs due to error {0}.", rpcException.Status);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to generate new IDs.");
            }
            finally
            {
                _generatedNewIDs.Release();
                Interlocked.Exchange(ref _isGeneratingNewIDs, False);
            }
        }

        private void InvalidateIDs(object state)
        {
            try
            {
                _idBuffer.Clear();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to invalidate IDs.");
            }
        }
    }
}
