using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Events
{
    public static class EventExtensions
    {
        public static IServiceCollection AddEvent<TEvent, TEventData>(this IServiceCollection services)
            where TEvent : class, IEventSource<TEventData>
        {
            return services
                .AddSingleton<TEvent>()
                .AddSingleton<IEventSource<TEventData>>(sp => sp.GetRequiredService<TEvent>())
                .AddSingleton<IEvent<TEventData>>(sp => sp.GetRequiredService<TEvent>());
        }
    }
}
