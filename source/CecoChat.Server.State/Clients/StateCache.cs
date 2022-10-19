using System.Diagnostics.CodeAnalysis;
using CecoChat.Contracts.State;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.State.Clients;

public interface IStateCache
{
    bool TryGetUserChat(long userID, string chatID, [NotNullWhen(returnValue: true)] out ChatState? chat);

    void AddUserChat(long userID, ChatState chat);
}

public sealed class LruStateCache : IStateCache
{
    private readonly LinkedList<CacheNode> _list;
    private readonly Dictionary<CacheKey, LinkedListNode<CacheNode>> _map;
    private readonly int _cacheCapacity;
    private SpinLock _lock;

    public LruStateCache(IOptions<StateCacheOptions> options)
    {
        _list = new();
        _map = new();
        _cacheCapacity = options.Value.Capacity;
        _lock = new();
    }

    public bool TryGetUserChat(long userID, string chatID, [NotNullWhen(returnValue: true)] out ChatState? chat)
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

    private bool TryGetUserChat(CacheKey key, [NotNullWhen(returnValue: true)] out ChatState? chat)
    {
        if (_map.TryGetValue(key, out LinkedListNode<CacheNode>? node))
        {
            chat = node.Value.Chat;
            // move as most recent
            _list.Remove(node);
            _list.AddFirst(node);

            return true;
        }

        chat = null;
        return false;
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
        if (_map.TryGetValue(key, out LinkedListNode<CacheNode>? node))
        {
            node.Value = new CacheNode(node.Value.UserID, chat);
            // move as most recent
            _list.Remove(node);
            _list.AddFirst(node);
        }
        else
        {
            CacheNode cacheNode = new(key.UserID, chat);
            node = _list.AddFirst(cacheNode);
            if (_list.Count > _cacheCapacity)
            {
                CacheNode lastCacheNode = _list.Last!.Value;
                // remove the least-recently used item when cache is full
                _list.RemoveLast();
                _map.Remove(new CacheKey(lastCacheNode.UserID, lastCacheNode.Chat.ChatId));
            }
            _map.Add(key, node);
        }
    }

    private readonly struct CacheNode
    {
        public CacheNode(long userID, ChatState chat)
        {
            if (userID == 0)
            {
                throw new ArgumentException($"{nameof(userID)} should be > 0.", nameof(userID));
            }
            if (chat == null)
            {
                throw new ArgumentNullException(nameof(chat), $"{nameof(chat)} should be non-null.");
            }

            UserID = userID;
            Chat = chat;
        }

        public long UserID { get; }
        public ChatState Chat { get; }
    }

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
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

            UserID = userID;
            ChatID = chatID;
        }

        public long UserID { get; }
        public string ChatID { get; }

        public bool Equals(CacheKey other)
        {
            return
                UserID == other.UserID &&
                string.Equals(ChatID, other.ChatID, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return
                obj is CacheKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();
            hashCode.Add(UserID);
            hashCode.Add(ChatID, StringComparer.InvariantCultureIgnoreCase);
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