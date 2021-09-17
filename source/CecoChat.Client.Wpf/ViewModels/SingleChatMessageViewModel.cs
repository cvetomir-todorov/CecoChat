using System;
using PropertyChanged;

namespace CecoChat.Client.Wpf.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public sealed class SingleChatMessageViewModel
    {
        public bool IsSenderCurrentUser { get; set; }

        public DateTime Timestamp { get; set; }

        public string FormattedMessage { get; set; }

        public string SequenceNumber { get; set; }

        public string DeliveryStatus { get; set; }
    }
}
