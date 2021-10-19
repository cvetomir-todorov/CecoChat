using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CecoChat.Client.Console
{
    public sealed class MessageStorage
    {
        private readonly long _userID;
        private readonly ConcurrentDictionary<long, Chat> _dialogMap;

        public MessageStorage(long userID)
        {
            _userID = userID;
            _dialogMap = new();
        }

        public void AddMessage(Message message)
        {
            long otherUserId = GetOtherUserID(message);
            Chat chat = _dialogMap.GetOrAdd(otherUserId, _ => new Chat());
            chat.AddNew(message);
        }

        public void AcknowledgeMessage(Message message)
        {
            long otherUserID = GetOtherUserID(message);
            Chat chat = _dialogMap.GetOrAdd(otherUserID, _ => new Chat());
            chat.UpdateDeliveryStatus(message);
        }

        public List<long> GetUsers()
        {
            return _dialogMap.Keys.ToList();
        }

        public List<Message> GetDialogMessages(long userID)
        {
            List<Message> messages = new();

            if (_dialogMap.TryGetValue(userID, out Chat dialog))
            {
                messages.AddRange(dialog.GetMessages());
            }

            return messages;
        }

        public bool TryGetMessage(long userID1, long userID2, long messageID, out Message message)
        {
            long otherUserID = GetOtherUserID(userID1, userID2);
            
            message = null;
            if (!_dialogMap.TryGetValue(otherUserID, out Chat dialog))
            {
                return false;
            }

            return dialog.TryGetMessage(messageID, out message);
        }

        private long GetOtherUserID(Message message)
        {
            return GetOtherUserID(message.SenderID, message.ReceiverID);
        }

        private long GetOtherUserID(long userID1, long userID2)
        {
            if (userID1 != _userID)
            {
                return userID1;
            }
            if (userID2 != _userID)
            {
                return userID2;
            }

            throw new InvalidOperationException($"Message is from current user {_userID} to himself.");
        }

        private sealed class Chat
        {
            private readonly ConcurrentDictionary<long, Message> _messageMap;

            public Chat()
            {
                _messageMap = new();
            }

            public void AddNew(Message message)
            {
                if (!_messageMap.TryGetValue(message.MessageID, out _))
                {
                    _messageMap.TryAdd(message.MessageID, message);
                }
            }

            public void UpdateDeliveryStatus(Message message)
            {
                if (_messageMap.TryGetValue(message.MessageID, out Message existing))
                {
                    existing.Status = message.Status;
                }
            }

            public IEnumerable<Message> GetMessages()
            {
                foreach (KeyValuePair<long,Message> pair in _messageMap)
                {
                    yield return pair.Value;
                }
            }

            public bool TryGetMessage(long messageID, out Message message)
            {
                return _messageMap.TryGetValue(messageID, out message);
            }
        }
    }
}