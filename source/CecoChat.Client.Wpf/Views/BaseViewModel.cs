using CecoChat.Client.Wpf.Clients.Messaging;
using CecoChat.Client.Wpf.Infrastructure;
using CecoChat.Client.Wpf.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.Wpf.Views
{
    public abstract class BaseViewModel
    {
        protected MessagingClient MessagingClient { get; }
        protected MessageStorage MessageStorage { get; }
        protected IDispatcher UIThreadDispatcher { get; }
        protected IErrorService ErrorService { get; }

        protected BaseViewModel()
        {
            MessagingClient = DIContainer.Services.GetRequiredService<MessagingClient>();
            MessageStorage = DIContainer.Services.GetRequiredService<MessageStorage>();
            UIThreadDispatcher = DIContainer.Services.GetRequiredService<IDispatcher>();
            ErrorService = DIContainer.Services.GetRequiredService<IErrorService>();
        }
    }
}
