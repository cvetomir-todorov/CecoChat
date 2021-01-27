using System;
using PropertyChanged;

namespace CecoChat.Client.Wpf.Views.SingleChat
{
    [AddINotifyPropertyChangedInterface]
    public sealed class SingleChatMessageViewModel
    {
        public bool IsSenderCurrentUser { get; set; }

        public DateTime Timestamp { get; set; }

        public string FormattedMessage { get; set; }
    }
}
