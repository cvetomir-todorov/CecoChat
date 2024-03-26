using Microsoft.Extensions.DependencyInjection;

namespace Common;

public interface IFactory<out TService>
{
    TService Create();
}

public sealed class Factory<TService> : IFactory<TService>
{
    private readonly Func<TService> _createFunction;

    public Factory(Func<TService> createFunction)
    {
        _createFunction = createFunction;
    }

    public TService Create()
    {
        return _createFunction();
    }
}

public static class FactoryRegistrations
{
    public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services
            .AddTransient<TService, TImplementation>()
            .AddSingleton<Func<TService>>(serviceProvider => () => serviceProvider.GetRequiredService<TService>())
            .AddSingleton<IFactory<TService>, Factory<TService>>();
    }
}
