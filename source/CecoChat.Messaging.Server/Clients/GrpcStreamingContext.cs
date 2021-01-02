using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CecoChat.Messaging.Server.Clients
{
    /// <summary>
    /// Streams <typeparam name="TMessage"/> instances to a connected client.
    /// </summary>
    public sealed class GrpcStreamingContext<TMessage> : IStreamingContext<TMessage>
    {
        private readonly ILogger _logger;
        private readonly IServerStreamWriter<TMessage> _responseStream;
        // TODO: consider adding queue size and drop messages if queue is full
        private readonly ConcurrentQueue<TMessage> _messageQueue;
        private readonly SemaphoreSlim _signalProcessing;

        public GrpcStreamingContext(ILogger logger, IServerStreamWriter<TMessage> responseStream)
        {
            _logger = logger;
            _responseStream = responseStream;
            _messageQueue = new ConcurrentQueue<TMessage>();
            _signalProcessing = new SemaphoreSlim(initialCount: 0, maxCount: 1);
        }

        public void Dispose()
        {
            _signalProcessing.Dispose();
        }

        public void AddMessage(TMessage message)
        {
            _messageQueue.Enqueue(message);
            _signalProcessing.Release();
        }

        public async Task ProcessMessages(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _signalProcessing.WaitAsync(ct);

                while (_messageQueue.TryDequeue(out TMessage message))
                {
                    try
                    {
                        await _responseStream.WriteAsync(message);
                        _logger.LogInformation("Success processing {0}", message);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Error processing {0}", message);
                    }
                }
            }
        }
    }
}