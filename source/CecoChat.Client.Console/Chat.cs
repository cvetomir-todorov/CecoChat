using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CecoChat.Client.Console
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

        /// <summary>
        /// The ID of the newest message in the chat.
        /// </summary>
        public long NewestMessage { get; set; }
        /// <summary>
        /// The ID of the last message delivered to the other user.
        /// </summary>
        public long OtherUserDelivered { get; set; }
        /// <summary>
        /// The ID of the last message seen by the other user.
        /// </summary>
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