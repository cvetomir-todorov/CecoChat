using System;
using PropertyChanged;

namespace CecoChat.Client.Wpf.Views.AllChats
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AllChatsItemViewModel
    {
        public long UserID { get; set; }

        public string LastMessage { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
