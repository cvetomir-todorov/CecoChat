using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Wpf.Infrastructure.Storage
{
    public sealed class DialogStorage
    {
        private readonly ConcurrentDictionary<string, Message> _messageMap;

        public DialogStorage()
        {
            _messageMap = new();
        }

        public void AddMessage(Message message)
        {
            _messageMap.TryAdd(message.MessageId, message);
        }

        public IEnumerable<Message> GetMessages()
        {
            foreach (KeyValuePair<string, Message> pair in _messageMap)
            {
                yield return pair.Value;
            }
        }
    }
}
