using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.State;

namespace CecoChat.Server.State.Clients
{
    public interface IStateCache
    {
        bool TryGetUserChat(long userID, string chatID, out ChatState chat);

        void UpdateUserChat(long userID, ChatState chat);
    }

    internal sealed class StateCache : IStateCache
    {
        private readonly ConcurrentDictionary<long, UserCache> _cache;

        public StateCache()
        {
            _cache = new();
        }

        public bool TryGetUserChat(long userID, string chatID, out ChatState chat)
        {
            if (_cache.TryGetValue(userID, out UserCache userCache))
            {
                return userCache.TryGetChat(chatID, out chat);
            }
            else
            {
                chat = null;
                return false;
            }
        }

        public void UpdateUserChat(long userID, ChatState chat)
        {
            _cache.AddOrUpdate(userID,
                _ => new UserCache(chat),
                (_, userCache) =>
                {
                    userCache.UpdateChat(chat);
                    return userCache;
                });
        }
    }

    internal sealed class UserCache
    {
        private readonly Dictionary<string, ChatState> _chats;

        public UserCache(ChatState chat)
        {
            _chats = new();
            _chats.Add(chat.ChatId, chat);
        }

        public bool TryGetChat(string chatID, out ChatState chat)
        {
            return _chats.TryGetValue(chatID, out chat);
        }

        public void UpdateChat(ChatState chat)
        {
            _chats.TryAdd(chat.ChatId, chat);
        }
    }
}