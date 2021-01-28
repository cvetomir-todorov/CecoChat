using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Shared.Storage
{
    public sealed class DialogStorage
    {
        private readonly ConcurrentDictionary<string, ClientMessage> _messageMap;

        public DialogStorage()
        {
            _messageMap = new();
        }

        public void AddMessage(ClientMessage message)
        {
            _messageMap.TryAdd(message.MessageId, message);
        }

        public IEnumerable<ClientMessage> GetMessages()
        {
            foreach (KeyValuePair<string, ClientMessage> pair in _messageMap)
            {
                yield return pair.Value;
            }
        }
    }
}
