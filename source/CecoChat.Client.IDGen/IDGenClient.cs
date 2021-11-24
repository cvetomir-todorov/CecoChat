﻿using System;
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
        private readonly IIDChannel _idChannel;
        private readonly Timer _invalidateIDsTimer;

        public IDGenClient(
            ILogger<IDGenClient> logger,
            IOptions<IDGenOptions> options,
            Contracts.IDGen.IDGen.IDGenClient client,
            IIDChannel idChannel)
        {
            _logger = logger;
            _options = options.Value;
            _client = client;
            _idChannel = idChannel;

            _logger.LogInformation("IDGen address set to {0}.", _options.Address);
            _invalidateIDsTimer = new(
                callback: _ => RefreshIDs(),
                state: null,
                dueTime: TimeSpan.Zero, 
                period: _options.RefreshIDsInterval);
        }

        public void Dispose()
        {
            _invalidateIDsTimer.Dispose();
        }

        public ValueTask<GetIDResult> GetID(CancellationToken ct)
        {
            if (!_idChannel.TryTakeID(out long id, _options.GetIDWaitInterval, ct))
            {
                _logger.LogWarning("Timed-out while waiting for new IDs to be generated.");
                return ValueTask.FromResult(new GetIDResult());
            }

            return ValueTask.FromResult(new GetIDResult {ID = id});
        }

        private void RefreshIDs()
        {
            // TODO: consider ensuring no more than 1 call is pending
            try
            {
                GenerateManyRequest request = new()
                {
                    OriginatorId = _options.OriginatorID,
                    Count = _options.RefreshIDsCount
                };
                DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);

                _idChannel.ClearIDs();
                GenerateManyResponse response = _client.GenerateMany(request, deadline: deadline);
                _idChannel.AddNewIDs(response.Ids);
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
