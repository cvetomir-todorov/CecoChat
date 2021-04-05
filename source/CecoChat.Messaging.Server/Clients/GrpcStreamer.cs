using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Grpc.Instrumentation;
using CecoChat.Tracing;
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
        private readonly IActivityUtility _activityUtility;
        private readonly IGrpcActivityUtility _grpcActivityUtility;
        private readonly BlockingCollection<MessageContext> _messageQueue;
        private readonly SemaphoreSlim _signalProcessing;

        private Func<TMessage, bool> _finalMessagePredicate;
        private IServerStreamWriter<TMessage> _streamWriter;
        private Guid _clientID;

        public GrpcStreamer(
            ILogger<GrpcStreamer<TMessage>> logger,
            IActivityUtility activityUtility,
            IGrpcActivityUtility grpcActivityUtility,
            IOptions<ClientOptions> options)
        {
            _logger = logger;
            _activityUtility = activityUtility;
            _grpcActivityUtility = grpcActivityUtility;

            _messageQueue = new(
                collection: new ConcurrentQueue<MessageContext>(),
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

        public bool EnqueueMessage(TMessage message, Activity parentActivity = null)
        {
            bool isAdded = _messageQueue.TryAdd(new MessageContext()
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

            while (!ct.IsCancellationRequested && _messageQueue.TryTake(out MessageContext messageContext))
            {
                Activity activity = StartActivity(messageContext.ParentActivity);
                bool success = false;

                try
                {
                    processedFinalMessage = _finalMessagePredicate(messageContext.Message);
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
                    _activityUtility.Stop(activity, success);
                }
            }

            return new EmptyQueueResult {Stop = processedFinalMessage};
        }

        // TODO: figure out how to make these generic or rename the class to something that reflects it
        private Activity StartActivity(Activity parentActivity)
        {
            Activity activity = null;
            if (parentActivity != null && _grpcActivityUtility.ActivitySource.HasListeners())
            {
                string name = $"{nameof(GrpcListenService)}.{nameof(GrpcListenService.Listen)}/Clients.PushMessage";
                activity = _grpcActivityUtility.ActivitySource.StartActivity(name, ActivityKind.Producer, parentActivity.Context);
            }

            _grpcActivityUtility.EnrichActivity(nameof(GrpcListenService), nameof(GrpcListenService.Listen), activity);
            return activity;
        }

        private record MessageContext
        {
            public TMessage Message { get; init; }

            public Activity ParentActivity { get; init; }
        }
    }
}