using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;
using CecoChat.Client.Wpf.Infrastructure;

namespace CecoChat.Client.Wpf.ViewModels
{
    // ReSharper disable once ArrangeModifiersOrder
    public abstract class BaseViewModel
    {
        protected MessagingClient MessagingClient { get; }
        protected MessageStorage MessageStorage { get; }
        protected IDispatcher UIThreadDispatcher { get; }
        protected IFeedbackService FeedbackService { get; }

        protected BaseViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IFeedbackService feedbackService)
        {
            MessagingClient = messagingClient;
            MessageStorage = messageStorage;
            UIThreadDispatcher = uiThreadDispatcher;
            FeedbackService = feedbackService;
        }
    }
}
