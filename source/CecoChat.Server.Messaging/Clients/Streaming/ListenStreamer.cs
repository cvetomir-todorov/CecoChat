using System.Collections.Concurrent;
using System.Diagnostics;
using CecoChat.Contracts.Messaging;
using CecoChat.Grpc.Telemetry;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Clients.Streaming;

public interface IListenStreamer : IStreamer<ListenNotification>
{
    void Initialize(Guid clientId, IServerStreamWriter<ListenNotification> streamWriter);
}

/// <summary>
/// Streams <see cref="ListenNotification"/> instances to a connected client.
/// </summary>
public sealed class ListenStreamer : IListenStreamer
{
    private readonly ILogger _logger;
    private readonly IGrpcStreamTelemetry _grpcStreamTelemetry;
    private readonly BlockingCollection<MessageContext> _messageQueue;
    private readonly SemaphoreSlim _signalProcessing;

    private IServerStreamWriter<ListenNotification>? _streamWriter;
    private Guid _clientId;
    private int _sequenceNumber;

    public ListenStreamer(
        ILogger<ListenStreamer> logger,
        IGrpcStreamTelemetry grpcStreamTelemetry,
        IOptions<ClientOptions> options)
    {
        _logger = logger;
        _grpcStreamTelemetry = grpcStreamTelemetry;

        ClientOptions clientOptions = options.Value;
        _messageQueue = new(
            collection: new ConcurrentQueue<MessageContext>(),
            boundedCapacity: clientOptions.SendMessagesHighWatermark);
        _signalProcessing = new SemaphoreSlim(initialCount: 0, maxCount: 1);
        _clientId = Guid.Empty;
    }

    public void Dispose()
    {
        _signalProcessing.Dispose();
        _messageQueue.Dispose();
    }

    public void Initialize(Guid clientId, IServerStreamWriter<ListenNotification> streamWriter)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(clientId)} should not be an empty GUID.", nameof(clientId));
        }

        _clientId = clientId;
        _streamWriter = streamWriter;
    }

    public Guid ClientID => _clientId;

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
            _logger.LogWarning("Dropped message {MessageId} of type {MessageType} since queue for client {ClientId} is full", message.MessageId, message.Type, ClientID);
        }

        return isAdded;
    }

    public async Task ProcessMessages(CancellationToken ct)
    {
        if (_clientId == Guid.Empty || _streamWriter == null)
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
                _logger.LogTrace("Sent client {ClientId} message {MessageId} of type {MessageType}",
          _clientId, messageContext.Message.MessageId, messageContext.Message.Type);
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
                _logger.LogError(exception, "Failed to send client {ClientId} message {MessageId} of type {MessageType}",
          _clientId, messageContext.Message.MessageId, messageContext.Message.Type);
                return new EmptyQueueResult { Stop = true };
            }
            finally
            {
                _grpcStreamTelemetry.StopStream(activity, success);
            }
        }

        return new EmptyQueueResult { Stop = processedFinalMessage };
    }

    private Activity StartActivity(ListenNotification message, Activity? parentActivity)
    {
        const string service = nameof(ListenService);
        const string method = nameof(ListenService.Listen);
        string name = $"{service}.{method}/Stream.{message.Type}";

        return _grpcStreamTelemetry.StartStream(name, service, method, parentActivity?.Context);
    }

    private sealed class MessageContext
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
