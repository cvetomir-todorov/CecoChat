using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Wpf.Infrastructure.Storage
{
    public sealed class DialogStorage
    {
        private readonly ConcurrentBag<Message> _messages;

        public DialogStorage()
        {
            _messages = new();
        }

        public void AddMessage(Message message)
        {
            _messages.Add(message);
        }

        public IEnumerable<Message> GetMessages()
        {
            foreach (Message message in _messages)
            {
                yield return message;
            }
        }
    }
}
