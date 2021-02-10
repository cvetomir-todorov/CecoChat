using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IGrpcStreamer<TMessage> : IStreamer<TMessage>
    {
        void Initialize(Guid clientID, IServerStreamWriter<TMessage> streamWriter);
    }

    /// <summary>
    /// Streams <typeparam name="TMessage"/> instances to a connected client.
    /// </summary>
    public sealed class GrpcStreamer<TMessage> : IGrpcStreamer<TMessage>
    {
        private readonly ILogger _logger;
        private readonly BlockingCollection<TMessage> _messageQueue;
        private readonly SemaphoreSlim _signalProcessing;

        private Func<TMessage, bool> _finalMessagePredicate;
        private IServerStreamWriter<TMessage> _streamWriter;
        private Guid _clientID;

        public GrpcStreamer(
            ILogger<GrpcStreamer<TMessage>> logger,
            IOptions<ClientOptions> options)
        {
            _logger = logger;
            _messageQueue = new BlockingCollection<TMessage>(
                collection: new ConcurrentQueue<TMessage>(),
                boundedCapacity: options.Value.SendMessagesHighWatermark);
            _signalProcessing = new SemaphoreSlim(initialCount: 0, maxCount: 1);
            _finalMessagePredicate = _ => false;
        }

        public void Initialize(Guid clientID, IServerStreamWriter<TMessage> streamWriter)
        {
            _clientID = clientID;
            _streamWriter = streamWriter;
        }

        public void Dispose()
        {
            _signalProcessing.Dispose();
        }

        public Guid ClientID => _clientID;

        public bool AddMessage(TMessage message)
        {
            bool isAdded = _messageQueue.TryAdd(message);
            if (isAdded)
            {
                _signalProcessing.Release();
            }
            else
            {
                _logger.LogTrace("Dropped message {0} since queue is full.", message);
            }

            return isAdded;
        }

        public async Task ProcessMessages(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _signalProcessing.WaitAsync(ct);
                EmptyQueueResult result = await EmptyQueue(ct);
                bool stop = result.Stop;
                if (stop)
                {
                    break;
                }
            }
        }

        public void SetFinalMessagePredicate(Func<TMessage, bool> finalMessagePredicate)
        {
            _finalMessagePredicate = finalMessagePredicate;
        }

        private struct EmptyQueueResult
        {
            public bool Stop { get; init; }
        }
 
        private async Task<EmptyQueueResult> EmptyQueue(CancellationToken ct)
        {
            bool processedFinalMessage = false;

            while (!ct.IsCancellationRequested && _messageQueue.TryTake(out TMessage message))
            {
                try
                {
                    processedFinalMessage = _finalMessagePredicate(message);
                    await _streamWriter.WriteAsync(message);
                    _logger.LogTrace("Sent {0} message {1}", _clientID, message);
                }
                catch (InvalidOperationException invalidOperationException)
                    when (invalidOperationException.Message == "Can't write the message because the request is complete.")
                {
                    // completed gRPC request is equivalent to client being disconnected
                    // even if underlying connection is still active
                    return new EmptyQueueResult {Stop = true};
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to send {0} message {1}", _clientID, message);
                    return new EmptyQueueResult {Stop = true};
                }
            }

            return new EmptyQueueResult {Stop = processedFinalMessage};
        }
    }
}