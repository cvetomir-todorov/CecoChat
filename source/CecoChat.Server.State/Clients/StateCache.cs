using System;
using System.Collections.Generic;
using System.Threading;
using CecoChat.Contracts.State;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.State.Clients
{
    public interface IStateCache
    {
        bool TryGetUserChat(long userID, string chatID, out ChatState chat);

        void AddUserChat(long userID, ChatState chat);
    }

    public sealed class LruStateCache : IStateCache
    {
        private readonly LinkedList<ChatState> _list;
        private readonly Dictionary<CacheKey, LinkedListNode<ChatState>> _map;
        private readonly int _cacheCapacity;
        private SpinLock _lock;

        public LruStateCache(IOptions<StateCacheOptions> options)
        {
            _list = new();
            _map = new();
            _cacheCapacity = options.Value.Capacity;
            _lock = new();
        }

        public bool TryGetUserChat(long userID, string chatID, out ChatState chat)
        {
            CacheKey key = new(userID, chatID);
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                return TryGetUserChat(key, out chat);
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit();
                }
            }
        }

        private bool TryGetUserChat(CacheKey key, out ChatState chat)
        {
            bool found = _map.TryGetValue(key, out LinkedListNode<ChatState> node);
            chat = found ? node.Value : null;
            if (found)
            {
                // move as most recent
                _list.Remove(node);
                _list.AddFirst(node);
            }

            return found;
        }

        public void AddUserChat(long userID, ChatState chat)
        {
            CacheKey key = new(userID, chat.ChatId);
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                AddUserChat(key, chat);
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit();
                }
            }
        }

        private void AddUserChat(CacheKey key, ChatState chat)
        {
            if (_map.TryGetValue(key, out LinkedListNode<ChatState> node))
            {
                node.Value = chat;
                // move as most recent
                _list.Remove(node);
                _list.AddFirst(node);
            }
            else
            {
                node = _list.AddFirst(chat);
                if (_list.Count > _cacheCapacity)
                {
                    // replace the least-recently used item with one that is most recent when cache is full
                    _list.RemoveLast();
                }
                _map.Add(key, node);
            }
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly long _userID;
            private readonly string _chatID;

            public CacheKey(long userID, string chatID)
            {
                if (userID == 0)
                {
                    throw new ArgumentException($"{nameof(userID)} should be > 0.", nameof(userID));
                }
                if (string.IsNullOrWhiteSpace(chatID))
                {
                    throw new ArgumentException($"{nameof(chatID)} should be non-null and non-whitespace.", nameof(chatID));
                }
                
                _userID = userID;
                _chatID = chatID;
            }

            public bool Equals(CacheKey other)
            {
                return
                    _userID == other._userID &&
                    string.Equals(_chatID, other._chatID, StringComparison.InvariantCultureIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return
                    obj is CacheKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                HashCode hashCode = new();
                hashCode.Add(_userID);
                hashCode.Add(_chatID, StringComparer.InvariantCultureIgnoreCase);
                return hashCode.ToHashCode();
            }

            public static bool operator ==(CacheKey left, CacheKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CacheKey left, CacheKey right)
            {
                return !left.Equals(right);
            }
        }
    }
}