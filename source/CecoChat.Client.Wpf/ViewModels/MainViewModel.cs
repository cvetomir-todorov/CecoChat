using System;
using CecoChat.Client.Shared;
using CecoChat.Client.Wpf.Infrastructure;
using CecoChat.Client.Wpf.Storage;
using PropertyChanged;

namespace CecoChat.Client.Wpf.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public sealed class MainViewModel : BaseViewModel
    {
        public MainViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IFeedbackService feedbackService,
            ConnectViewModel connectVM,
            AllChatsViewModel allChatsVM)
            : base(messagingClient, messageStorage, uiThreadDispatcher, feedbackService)
        {
            MessagingClient.ExceptionOccurred += MessagingClientOnExceptionOccurred;
            MessagingClient.Disconnected += MessagingClientOnDisconnected;

            ConnectVM = connectVM;
            ConnectVM.CanOperate = true;
            ConnectVM.Connected += ConnectViewModelOnConnected;

            AllChatsVM = allChatsVM;
            AllChatsVM.CanOperate = false;
        }

        public ConnectViewModel ConnectVM { get; }

        public AllChatsViewModel AllChatsVM { get; }

        private void MessagingClientOnExceptionOccurred(object sender, Exception exception)
        {
            FeedbackService.ShowError(exception);
            AllChatsVM.CanOperate = false;
            ConnectVM.CanOperate = true;
        }

        private void MessagingClientOnDisconnected(object sender, EventArgs e)
        {
            FeedbackService.ShowWarning("Disconnected.");
            AllChatsVM.CanOperate = false;
            ConnectVM.CanOperate = true;
        }

        private void ConnectViewModelOnConnected(object sender, EventArgs e)
        {
            ConnectVM.CanOperate = false;
            AllChatsVM.CanOperate = true;
            AllChatsVM.Start();
        }
    }
}
