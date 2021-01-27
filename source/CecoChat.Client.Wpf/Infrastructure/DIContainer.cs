using System;
using CecoChat.Client.Wpf.Clients.Messaging;
using CecoChat.Client.Wpf.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.Wpf.Infrastructure
{
    public static class DIContainer
    {
        public static void Initialize()
        {
            ServiceCollection services = new();

            services.AddSingleton<MessagingClient>();
            services.AddSingleton<MessageIDGenerator>();
            services.AddSingleton<MessageStorage>();
            services.AddSingleton<IDispatcher, WpfUIThreadDispatcher>();
            services.AddSingleton<IErrorService, WpfErrorService>();

            Services = services.BuildServiceProvider();
        }

        public static IServiceProvider Services { get; private set; }
    }
}
