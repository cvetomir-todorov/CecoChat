using System;
using System.Threading;
using System.Threading.Tasks;
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
            IFeedbackService feedbackService)
            : base(messagingClient, messageStorage, uiThreadDispatcher, feedbackService)
        {
            CanOperate = true;
            UserID = "1";
            ConnectServer = "https://localhost:31000";
            Connect = new AsyncRelayCommand(ConnectExecuted);
        }

        public bool CanOperate { get; set; }

        public string UserID { get; set; }

        public string ConnectServer { get; set; }

        public ICommand Connect { get; }

        public event EventHandler Connected;

        private async Task ConnectExecuted()
        {
            try
            {
                await MessagingClient.Initialize(long.Parse(UserID), ConnectServer);
                MessagingClient.ListenForMessages(CancellationToken.None);

                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                FeedbackService.ShowError(exception);
            }
        }
    }
}
