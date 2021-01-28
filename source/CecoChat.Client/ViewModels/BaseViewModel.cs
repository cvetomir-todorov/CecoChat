using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;

namespace CecoChat.Client.ViewModels
{
    public abstract class BaseViewModel
    {
        protected MessagingClient MessagingClient { get; }
        protected MessageStorage MessageStorage { get; }
        protected IDispatcher UIThreadDispatcher { get; }
        protected IErrorService ErrorService { get; }

        protected BaseViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IErrorService errorService)
        {
            MessagingClient = messagingClient;
            MessageStorage = messageStorage;
            UIThreadDispatcher = uiThreadDispatcher;
            ErrorService = errorService;
        }
    }
}
