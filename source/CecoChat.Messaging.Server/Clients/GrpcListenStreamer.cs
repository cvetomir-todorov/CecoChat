using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.Grpc.Instrumentation;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IGrpcListenStreamer : IStreamer<ListenResponse>
    {
        void Initialize(Guid clientID, IServerStreamWriter<ListenResponse> streamWriter, IStreamingStrategy streamingStrategy);
    }

    public interface IStreamingStrategy
    {
        bool IsFinal(ListenResponse response);

        bool AffectsSequencing(ListenResponse response);
    }

    /// <summary>
    /// Streams <see cref="ListenResponse"/> instances to a connected client.
    /// </summary>
    public sealed class GrpcListenStreamer : IGrpcListenStreamer
    {
        private readonly ILogger _logger;
        private readonly IGrpcActivityUtility _grpcActivityUtility;
        private readonly BlockingCollection<MessageContext> _messageQueue;
        private readonly SemaphoreSlim _signalProcessing;

        private IServerStreamWriter<ListenResponse> _streamWriter;
        private IStreamingStrategy _streamingStrategy;
        private Guid _clientID;
        private int _sequenceNumber;

        public GrpcListenStreamer(
            ILogger<GrpcListenStreamer> logger,
            IGrpcActivityUtility grpcActivityUtility,
            IOptions<ClientOptions> options)
        {
            _logger = logger;
            _grpcActivityUtility = grpcActivityUtility;

            ClientOptions clientOptions = options.Value;
            _messageQueue = new(
                collection: new ConcurrentQueue<MessageContext>(),
                boundedCapacity: clientOptions.SendMessagesHighWatermark);
            _signalProcessing = new SemaphoreSlim(initialCount: 0, maxCount: 1);
            _streamingStrategy = DefaultStreamingStrategy.Instance;
        }

        public void Dispose()
        {
            _signalProcessing.Dispose();
            _messageQueue.Dispose();
        }

        public void Initialize(Guid clientID, IServerStreamWriter<ListenResponse> streamWriter, IStreamingStrategy streamingStrategy)
        {
            _clientID = clientID;
            _streamWriter = streamWriter;
            _streamingStrategy = streamingStrategy;
        }

        public Guid ClientID => _clientID;

        public bool EnqueueMessage(ListenResponse message, Activity parentActivity = null)
        {
            if (_streamingStrategy.AffectsSequencing(message))
            {
                _sequenceNumber++;
            }

            bool isAdded = _messageQueue.TryAdd(new MessageContext
            {
                Message = message,
                ParentActivity = parentActivity
            });

            if (isAdded)
            {
                _signalProcessing.Release();
            }
            else
            {
                _logger.LogWarning("Dropped message {0} since queue for {1} is full.", message, ClientID);
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

        private struct EmptyQueueResult
        {
            public bool Stop { get; init; }
        }
 
        private async Task<EmptyQueueResult> EmptyQueue(CancellationToken ct)
        {
            bool processedFinalMessage = false;

            while (!ct.IsCancellationRequested && _messageQueue.TryTake(out MessageContext messageContext))
            {
                Activity activity = StartActivity(messageContext.Message, messageContext.ParentActivity);
                bool success = false;

                try
                {
                    processedFinalMessage = _streamingStrategy.IsFinal(messageContext.Message);
                    messageContext.Message.SequenceNumber = _sequenceNumber;
                    await _streamWriter.WriteAsync(messageContext.Message);
                    success = true;
                    _logger.LogTrace("Sent {0} message {1}", _clientID, messageContext.Message);
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
                    _logger.LogError(exception, "Failed to send {0} message {1}", _clientID, messageContext.Message);
                    return new EmptyQueueResult {Stop = true};
                }
                finally
                {
                    _grpcActivityUtility.Stop(activity, success);
                }
            }

            return new EmptyQueueResult {Stop = processedFinalMessage};
        }

        private Activity StartActivity(ListenResponse message, Activity parentActivity)
        {
            const string service = nameof(GrpcListenService);
            const string method = nameof(GrpcListenService.Listen);
            string name = $"{service}.{method}/StreamMessage.{message.Message.Type}";

            return _grpcActivityUtility.StartServiceMethod(name, service, method, parentActivity.Context);
        }

        private record MessageContext
        {
            public ListenResponse Message { get; init; }

            public Activity ParentActivity { get; init; }
        }

        private sealed class DefaultStreamingStrategy : IStreamingStrategy
        {
            public static readonly DefaultStreamingStrategy Instance = new();

            private DefaultStreamingStrategy()
            {}

            public bool IsFinal(ListenResponse response)
            {
                return false;
            }

            public bool AffectsSequencing(ListenResponse response)
            {
                return true;
            }
        }
    }
}