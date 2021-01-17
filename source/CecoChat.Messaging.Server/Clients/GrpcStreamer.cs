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
    public sealed class GrpcStreamer<TMessage> : IStreamer<TMessage>
    {
        private readonly ILogger _logger;
        private readonly IServerStreamWriter<TMessage> _streamWriter;
        private readonly string _clientID;
        // TODO: consider adding queue size and drop messages if queue is full
        private readonly ConcurrentQueue<TMessage> _messageQueue;
        private readonly SemaphoreSlim _signalProcessing;

        public GrpcStreamer(
            ILogger logger,
            IServerStreamWriter<TMessage> streamWriter,
            ServerCallContext context)
        {
            _logger = logger;
            _streamWriter = streamWriter;
            _clientID = context.Peer;
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
                        await _streamWriter.WriteAsync(message);
                        _logger.LogTrace("Sent {0} message {1}", _clientID, message);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Failed to send {0} message {1}", _clientID, message);
                    }
                }
            }
        }
    }
}