using System;
using CecoChat.Client.Wpf.Views.AllChats;
using CecoChat.Client.Wpf.Views.Connect;
using PropertyChanged;

namespace CecoChat.Client.Wpf.Views.Main
{
    [AddINotifyPropertyChangedInterface]
    public sealed class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            MessagingClient.ExceptionOccurred += MessagingClientOnExceptionOccurred;

            ConnectVM = new();
            ConnectVM.CanOperate = true;
            ConnectVM.Connected += ConnectViewModelOnConnected;

            AllChatsVM = new();
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
