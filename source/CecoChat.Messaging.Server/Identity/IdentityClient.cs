using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using CecoChat.Messaging.Server.Clients;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityClient : IDisposable
    {
        ValueTask<GetIdentityResult> GetIdentity(long userID, CancellationToken ct);
    }

    public struct GetIdentityResult
    {
        public bool Success => ID > 0;
        public long ID { get; set; }
    }

    public sealed class IdentityClient : IIdentityClient
    {
        private readonly ILogger _logger;
        private readonly IIdentityOptions _options;
        private readonly Contracts.Identity.Identity.IdentityClient _client;
        private readonly ConcurrentBag<long> _idBuffer;
        private readonly SemaphoreSlim _generatedNewIDs;
        private readonly Timer _invalidateIDsTimer;
        private int _isGeneratingNewIDs;
        private const int True = 1;
        private const int False = 0;

        public IdentityClient(
            ILogger<IdentityClient> logger,
            IOptions<IdentityOptions> identityOptions,
            IOptions<ClientOptions> clientOptions,
            Contracts.Identity.Identity.IdentityClient client)
        {
            _logger = logger;
            _options = identityOptions.Value;
            _client = client;

            _logger.LogInformation("Identity address set to {0}.", _options.Communication.Address);
            _idBuffer = new();
            _generatedNewIDs = new(initialCount: 0, maxCount: clientOptions.Value.MaxClients);
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

        public async ValueTask<GetIdentityResult> GetIdentity(long userID, CancellationToken ct)
        {
            if (_idBuffer.TryTake(out long id))
            {
                return new GetIdentityResult {ID = id};
            }

            TriggerNewIDGeneration(userID, _options.Generation.RefreshIDsCount);
            if (!await _generatedNewIDs.WaitAsync(_options.Generation.GetIDWaitInterval, ct))
            {
                _logger.LogWarning("Timed-out while waiting for new IDs to be generated.");
                return new GetIdentityResult();
            }

            if (!_idBuffer.TryTake(out id))
            {
                _logger.LogWarning("No ID available after ensuring generating new IDs is triggered.");
                return new GetIdentityResult();
            }

            return new GetIdentityResult {ID = id};
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
                    _idBuffer.Add(id);
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
