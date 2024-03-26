using Autofac;

namespace Common.Events;

public static class EventRegistrations
{
    public static void RegisterSingletonEvent<TEvent, TEventData>(this ContainerBuilder builder)
        where TEvent : class, IEventSource<TEventData>
    {
        builder.RegisterType<TEvent>()
            .As<IEventSource<TEventData>>()
            .As<IEvent<TEventData>>()
            .SingleInstance();
    }
}
