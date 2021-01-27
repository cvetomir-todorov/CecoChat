using System.Collections.Generic;
using System.Collections.ObjectModel;
using CecoChat.Contracts.Client;
using PropertyChanged;

namespace CecoChat.Client.Wpf.Views.SingleChat
{
    [AddINotifyPropertyChangedInterface]
    public sealed class SingleChatViewModel : BaseViewModel
    {
        public SingleChatViewModel()
        {
            MessagingClient.MessageReceived += MessagingClientOnMessageReceived;
            Messages = new ObservableCollection<SingleChatMessageViewModel>();
        }

        public long OtherUserID { get; set; }

        public ObservableCollection<SingleChatMessageViewModel> Messages { get; }

        private void MessagingClientOnMessageReceived(object sender, Message message)
        {
            if (message.SenderId != OtherUserID && message.ReceiverId != OtherUserID)
            {
                return;
            }

            InsertMessage(message);
        }

        public void SetOtherUser(long otherUserID)
        {
            OtherUserID = otherUserID;

            Messages.Clear();
            IEnumerable<Message> messages = MessageStorage.GetMessages(otherUserID);
            foreach (Message message in messages)
            {
                InsertMessage(message);
            }
        }

        private void InsertMessage(Message message)
        {
            int insertionIndex = 0;

            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                if (message.Timestamp.ToDateTime() > Messages[i].Timestamp)
                {
                    insertionIndex = i + 1;
                    break;
                }
            }

            SingleChatMessageViewModel messageVM = new()
            {
                IsSenderCurrentUser = message.SenderId == MessagingClient.UserID,
                Timestamp = message.Timestamp.ToDateTime(),
                FormattedMessage = $"[{message.Timestamp.ToDateTime()}] {message.SenderId}: {message.PlainTextData.Text}"
            };

            if (insertionIndex >= Messages.Count)
            {
                UIThreadDispatcher.Invoke(() => Messages.Add(messageVM));
            }
            else
            {
                UIThreadDispatcher.Invoke(() => Messages.Insert(insertionIndex, messageVM));
            }
        }
    }
}
