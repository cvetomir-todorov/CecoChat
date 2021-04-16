using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;
using CecoChat.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client
{
    public static class Registrations
    {
        public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
        {
            services.AddSingleton<MessagingClient>();
            services.AddSingleton<MessageStorage>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<ConnectViewModel>();
            services.AddSingleton<AllChatsViewModel>();
            services.AddSingleton<SingleChatViewModel>();

            return services;
        }
    }
}
