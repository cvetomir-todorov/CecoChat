﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Wpf.Storage
{
    public sealed class MessageStorage
    {
        private readonly ConcurrentDictionary<long, DialogStorage> _dialogMap;

        public MessageStorage()
        {
            _dialogMap = new();
        }

        public void AddMessage(long userID, ClientMessage message)
        {
            DialogStorage dialogStorage = _dialogMap.GetOrAdd(userID, _ => new DialogStorage());
            dialogStorage.AddMessage(message);
        }

        public IEnumerable<ClientMessage> GetMessages(long userID)
        {
            DialogStorage dialogStorage = _dialogMap.GetOrAdd(userID, _ => new DialogStorage());
            return dialogStorage.GetMessages();
        }
    }
}
