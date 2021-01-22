using System;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.DependencyInjection
{
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

    public static class FactoryExtensions
    {
        public static void AddFactory<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddTransient<TService, TImplementation>();
            services.AddSingleton<Func<TService>>(serviceProvider => () => serviceProvider.GetRequiredService<TService>());
            services.AddSingleton<IFactory<TService>, Factory<TService>>();
        }
    }
}
