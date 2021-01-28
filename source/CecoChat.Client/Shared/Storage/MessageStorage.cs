using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Shared.Storage
{
    public sealed class MessageStorage
    {
        private readonly ConcurrentDictionary<long, DialogStorage> _dialogMap;

        public MessageStorage()
        {
            _dialogMap = new();
        }

        public void AddMessage(long userID, Message message)
        {
            DialogStorage dialogStorage = _dialogMap.GetOrAdd(userID, _ => new DialogStorage());
            dialogStorage.AddMessage(message);
        }

        public IEnumerable<Message> GetMessages(long userID)
        {
            DialogStorage dialogStorage = _dialogMap.GetOrAdd(userID, _ => new DialogStorage());
            return dialogStorage.GetMessages();
        }
    }
}
