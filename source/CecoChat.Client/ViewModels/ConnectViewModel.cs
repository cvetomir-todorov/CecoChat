using System;
using System.Threading;
using System.Windows.Input;
using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;

namespace CecoChat.Client.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public sealed class ConnectViewModel : BaseViewModel
    {
        public ConnectViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IErrorService errorService)
            : base(messagingClient, messageStorage, uiThreadDispatcher, errorService)
        {
            CanOperate = true;
            UserID = "1";
            Server = "https://localhost:31001";
            Connect = new RelayCommand(ConnectExecuted);
        }

        public bool CanOperate { get; set; }

        public string UserID { get; set; }

        public string Server { get; set; }

        public ICommand Connect { get; }

        public event EventHandler Connected;

        private void ConnectExecuted()
        {
            MessagingClient.Initialize(long.Parse(UserID), Server);
            // TODO: pass a real cancellation token which gets cancelled when app is shut down or a critical exception happens
            MessagingClient.ListenForMessages(CancellationToken.None);

            Connected?.Invoke(this, EventArgs.Empty);
        }
    }
}
