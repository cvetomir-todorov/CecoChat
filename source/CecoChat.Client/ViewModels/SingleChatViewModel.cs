using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CecoChat.Client.Shared;
using CecoChat.Client.Shared.Storage;
using CecoChat.Contracts;
using CecoChat.Contracts.Client;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;

namespace CecoChat.Client.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public sealed class SingleChatViewModel : BaseViewModel
    {
        private readonly HashSet<Guid> _messageIDs;

        public SingleChatViewModel(
            MessagingClient messagingClient,
            MessageStorage messageStorage,
            IDispatcher uiThreadDispatcher,
            IFeedbackService feedbackService)
            : base(messagingClient, messageStorage, uiThreadDispatcher, feedbackService)
        {
            _messageIDs = new();

            MessagingClient.MessageReceived += MessagingClientOnMessageReceived;
            Messages = new ObservableCollection<SingleChatMessageViewModel>();
            SendMessage = new AsyncRelayCommand(SendMessageExecuted);
        }

        public long OtherUserID { get; set; }

        public ObservableCollection<SingleChatMessageViewModel> Messages { get; }

        public bool CanSend { get; set; }

        public string MessageText { get; set; }

        public ICommand SendMessage { get; }

        public event EventHandler<ClientMessage> MessageSent;

        private void MessagingClientOnMessageReceived(object sender, ClientMessage message)
        {
            if (message.SenderId != OtherUserID && message.ReceiverId != OtherUserID)
            {
                return;
            }

            InsertMessage(message);
        }

        private async Task SendMessageExecuted()
        {
            try
            {
                ClientMessage message = await MessagingClient.SendPlainTextMessage(OtherUserID, MessageText);
                MessageStorage.AddMessage(OtherUserID, message);
                InsertMessage(message);
                MessageText = string.Empty;
                MessageSent?.Invoke(this, message);
            }
            catch (Exception exception)
            {
                FeedbackService.ShowError(exception);
            }
        }

        public async Task SetOtherUser(long otherUserID)
        {
            OtherUserID = otherUserID;

            _messageIDs.Clear();
            Messages.Clear();

            IEnumerable<ClientMessage> storedMessages = MessageStorage.GetMessages(otherUserID);
            IList<ClientMessage> dialogHistory = await MessagingClient.GetDialogHistory(otherUserID, DateTime.UtcNow);
            IEnumerable<ClientMessage> allMessages = storedMessages.Union(dialogHistory);

            foreach (ClientMessage message in allMessages)
            {
                InsertMessage(message);
            }

            CanSend = true;
        }

        private void InsertMessage(ClientMessage message)
        {
            Guid messageID = message.MessageId.ToGuid();
            if (_messageIDs.Contains(messageID))
                return;

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
                FormattedMessage = $"[{message.Timestamp.ToDateTime()}] {message.SenderId}: {message.Text}"
            };

            if (insertionIndex >= Messages.Count)
            {
                UIThreadDispatcher.Invoke(() => Messages.Add(messageVM));
            }
            else
            {
                UIThreadDispatcher.Invoke(() => Messages.Insert(insertionIndex, messageVM));
            }

            _messageIDs.Add(messageID);
        }
    }
}
