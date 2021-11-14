using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CecoChat.Contracts.State;

namespace CecoChat.Server.State.Clients
{
    // TODO: use a real cache with an eviction policy, consider moving it elsewhere
    public interface IStateCache
    {
        IReadOnlyCollection<ChatState> GetUserChats(long userID);

        bool TryGetUserChat(long userID, string chatID, out ChatState chat);

        void UpdateUserChats(long userID, IEnumerable<ChatState> chats);

        void UpdateUserChat(long userID, ChatState chat);
    }

    internal sealed class StateCache : IStateCache
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<ChatState> _emptyChats = new(capacity: 0);

        private readonly ConcurrentDictionary<long, UserCache> _cache;

        public StateCache()
        {
            _cache = new();
        }

        public IReadOnlyCollection<ChatState> GetUserChats(long userID)
        {
            if (_cache.TryGetValue(userID, out UserCache userCache))
            {
                return userCache.GetChats();
            }
            else
            {
                return _emptyChats;
            }
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

        public void UpdateUserChats(long userID, IEnumerable<ChatState> chats)
        {
            _cache.AddOrUpdate(userID,
                _ => new UserCache(chats),
                (_, userCache) =>
                {
                    userCache.UpdateChats(chats);
                    return userCache;
                });
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

        public UserCache(IEnumerable<ChatState> chats)
        {
            _chats = new();
            foreach (ChatState chat in chats)
            {
                _chats.Add(chat.ChatId, chat);
            }
        }

        public UserCache(ChatState chat)
        {
            _chats = new();
            _chats.Add(chat.ChatId, chat);
        }

        public IReadOnlyCollection<ChatState> GetChats()
        {
            return _chats.Select(pair => pair.Value).ToList();
        }

        public bool TryGetChat(string chatID, out ChatState chat)
        {
            return _chats.TryGetValue(chatID, out chat);
        }

        public void UpdateChats(IEnumerable<ChatState> chats)
        {
            foreach (ChatState chat in chats)
            {
                _chats.TryAdd(chat.ChatId, chat);
            }
        }

        public void UpdateChat(ChatState chat)
        {
            _chats.TryAdd(chat.ChatId, chat);
        }
    }
}