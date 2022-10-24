using System.Collections.Concurrent;
using System.Diagnostics;
using CecoChat.Contracts.Messaging;
using CecoChat.Grpc.Instrumentation;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Clients.Streaming;

public interface IGrpcListenStreamer : IStreamer<ListenNotification>
{
    void Initialize(Guid clientID, IServerStreamWriter<ListenNotification> streamWriter);
}

/// <summary>
/// Streams <see cref="ListenNotification"/> instances to a connected client.
/// </summary>
public sealed class GrpcListenStreamer : IGrpcListenStreamer
{
    private readonly ILogger _logger;
    private readonly IGrpcStreamingActivityUtility _grpcStreamingActivityUtility;
    private readonly BlockingCollection<MessageContext> _messageQueue;
    private readonly SemaphoreSlim _signalProcessing;

    private IServerStreamWriter<ListenNotification>? _streamWriter;
    private Guid _clientID;
    private int _sequenceNumber;

    public GrpcListenStreamer(
        ILogger<GrpcListenStreamer> logger,
        IGrpcStreamingActivityUtility grpcStreamingActivityUtility,
        IOptions<ClientOptions> options)
    {
        _logger = logger;
        _grpcStreamingActivityUtility = grpcStreamingActivityUtility;

        ClientOptions clientOptions = options.Value;
        _messageQueue = new(
            collection: new ConcurrentQueue<MessageContext>(),
            boundedCapacity: clientOptions.SendMessagesHighWatermark);
        _signalProcessing = new SemaphoreSlim(initialCount: 0, maxCount: 1);
        _clientID = Guid.Empty;
    }

    public void Dispose()
    {
        _signalProcessing.Dispose();
        _messageQueue.Dispose();
    }

    public void Initialize(Guid clientID, IServerStreamWriter<ListenNotification> streamWriter)
    {
        if (clientID == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(clientID)} should not be an empty GUID.", nameof(clientID));
        }

        _clientID = clientID;
        _streamWriter = streamWriter;
    }

    public Guid ClientID => _clientID;

    public bool EnqueueMessage(ListenNotification message, Activity? parentActivity = null)
    {
        _sequenceNumber++;

        bool isAdded = _messageQueue.TryAdd(new MessageContext(message, parentActivity));
        if (isAdded)
        {
            _signalProcessing.Release();
        }
        else
        {
            _logger.LogWarning("Dropped message {@Message} since queue for {ClientId} is full", message, ClientID);
        }

        return isAdded;
    }

    public async Task ProcessMessages(CancellationToken ct)
    {
        if (_clientID == Guid.Empty || _streamWriter == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' before '{nameof(ProcessMessages)}'.");
        }

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

        while (!ct.IsCancellationRequested && _messageQueue.TryTake(out MessageContext? messageContext))
        {
            Activity activity = StartActivity(messageContext.Message, messageContext.ParentActivity);
            bool success = false;

            try
            {
                processedFinalMessage = messageContext.Message.Type == MessageType.Disconnect;
                messageContext.Message.SequenceNumber = _sequenceNumber;
                await _streamWriter!.WriteAsync(messageContext.Message, ct);
                success = true;
                _logger.LogTrace("Sent client {ClientId} message {@Message}", _clientID, messageContext.Message);
            }
            catch (InvalidOperationException invalidOperationException)
                when (invalidOperationException.Message == "Can't write the message because the request is complete.")
            {
                // completed gRPC request is equivalent to client being disconnected
                // even if underlying connection is still active
                return new EmptyQueueResult { Stop = true };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send client {ClientId} message {@Message}", _clientID, messageContext.Message);
                return new EmptyQueueResult { Stop = true };
            }
            finally
            {
                _grpcStreamingActivityUtility.Stop(activity, success);
            }
        }

        return new EmptyQueueResult { Stop = processedFinalMessage };
    }

    private Activity StartActivity(ListenNotification message, Activity? parentActivity)
    {
        const string service = nameof(GrpcListenService);
        const string method = nameof(GrpcListenService.Listen);
        string name = $"{service}.{method}/Stream.{message.Type}";

        return _grpcStreamingActivityUtility.StartStreaming(name, service, method, parentActivity?.Context);
    }

    private sealed record MessageContext
    {
        public MessageContext(ListenNotification message, Activity? parentActivity)
        {
            Message = message;
            ParentActivity = parentActivity;
        }

        public ListenNotification Message { get; }

        public Activity? ParentActivity { get; }
    }
}