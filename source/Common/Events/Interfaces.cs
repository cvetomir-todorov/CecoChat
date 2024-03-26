namespace Common.Events;

public interface ISubscriber<in TEventData>
{
    ValueTask Handle(TEventData eventData);
}

public interface IEvent<out TEventData>
{
    Guid Subscribe(ISubscriber<TEventData> subscriber);

    Guid Subscribe(ISubscriber<TEventData> subscriber, Func<TEventData, bool> condition);

    bool TryUnsubscribe(Guid token);

    void Unsubscribe(Guid token);
}

public interface IEventSource<TEventData> : IEvent<TEventData>, IDisposable
{
    void Publish(TEventData eventData);
}
