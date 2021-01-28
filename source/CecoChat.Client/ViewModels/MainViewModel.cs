using System;
using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;
using PropertyChanged;

namespace CecoChat.Client.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public sealed class MainViewModel : BaseViewModel
    {
        public MainViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IErrorService errorService,
            ConnectViewModel connectVM,
            AllChatsViewModel allChatsVM)
            : base(messagingClient, messageStorage, uiThreadDispatcher, errorService)
        {
            MessagingClient.ExceptionOccurred += MessagingClientOnExceptionOccurred;

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
            ErrorService.ShowError(exception);
        }

        private void ConnectViewModelOnConnected(object sender, EventArgs e)
        {
            ConnectVM.CanOperate = false;
            AllChatsVM.CanOperate = true;
            AllChatsVM.Start();
        }
    }
}
