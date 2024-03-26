using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Common.Events;

public class EventSource<TEventData> : IEventSource<TEventData>
{
    private static bool AlwaysTrue(TEventData _) { return true; }

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, SubscriberInfo> _subscribersMap;

    public EventSource(ILogger<EventSource<TEventData>> logger)
    {
        _logger = logger;
        _subscribersMap = new();
    }

    public void Dispose()
    {
        Dispose(isDisposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    { }

    public Guid Subscribe(ISubscriber<TEventData> subscriber)
    {
        return Subscribe(subscriber, AlwaysTrue);
    }

    public Guid Subscribe(ISubscriber<TEventData> subscriber, Func<TEventData, bool> condition)
    {
        Guid token = Guid.NewGuid();
        SubscriberInfo subscriberInfo = new(subscriber, condition);
        if (!_subscribersMap.TryAdd(token, subscriberInfo))
        {
            throw new InvalidOperationException($"Could not add subscriber since token {token} is already present.");
        }

        return token;
    }

    public bool TryUnsubscribe(Guid token)
    {
        return _subscribersMap.TryRemove(token, out _);
    }

    public void Unsubscribe(Guid token)
    {
        if (!_subscribersMap.TryRemove(token, out _))
        {
            throw new InvalidOperationException($"Could not unsubscribe since token {token} is not present.");
        }
    }

    public void Publish(TEventData eventData)
    {
        Task.Run(async () => await ProcessEvent(eventData));
    }

    private async Task ProcessEvent(TEventData eventData)
    {
        foreach (SubscriberInfo context in _subscribersMap.Select(pair => pair.Value))
        {
            try
            {
                if (context.Condition(eventData))
                {
                    await context.Subscriber.Handle(eventData);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while a subscriber was handling event");
            }
        }
    }

    private sealed class SubscriberInfo
    {
        public SubscriberInfo(ISubscriber<TEventData> subscriber, Func<TEventData, bool> condition)
        {
            Subscriber = subscriber;
            Condition = condition;
        }

        public ISubscriber<TEventData> Subscriber { get; }
        public Func<TEventData, bool> Condition { get; }
    }
}
