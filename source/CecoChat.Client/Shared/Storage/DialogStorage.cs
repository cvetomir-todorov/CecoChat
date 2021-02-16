using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Shared.Storage
{
    public sealed class DialogStorage
    {
        private readonly ConcurrentDictionary<Guid, ClientMessage> _messageMap;

        public DialogStorage()
        {
            _messageMap = new();
        }

        public void AddMessage(ClientMessage message)
        {
            _messageMap.TryAdd(message.MessageId.ToGuid(), message);
        }

        public IEnumerable<ClientMessage> GetMessages()
        {
            foreach (KeyValuePair<Guid, ClientMessage> pair in _messageMap)
            {
                yield return pair.Value;
            }
        }
    }
}
