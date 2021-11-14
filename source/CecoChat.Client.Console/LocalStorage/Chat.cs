using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CecoChat.Client.Console.LocalStorage
{
    public sealed class Chat
    {
        private readonly ConcurrentDictionary<long, Message> _messageMap;

        public Chat(long otherUserID)
        {
            OtherUserID = otherUserID;
            _messageMap = new();
        }

        public long OtherUserID { get; }

        // The ID of the newest message in the chat.
        public long NewestMessage { get; set; }

        // The ID of the last processed message. 
        public long Processed { get; set; }

        // The ID of the last message delivered to the other user.
        public long OtherUserDelivered { get; set; }

        // The ID of the last message seen by the other user.
        public long OtherUserSeen { get; set; }

        public void AddNew(Message message)
        {
            if (!_messageMap.TryGetValue(message.MessageID, out _))
            {
                _messageMap.TryAdd(message.MessageID, message);
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