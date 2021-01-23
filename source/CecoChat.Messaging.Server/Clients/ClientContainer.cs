using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;
using ConcurrentCollections;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        void AddClient(in long userID, IStreamer<ListenResponse> streamer);

        void RemoveClient(in long userID, IStreamer<ListenResponse> streamer);

        IEnumerable<IStreamer<ListenResponse>> GetClients(in long userID);
    }

    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamer<ListenResponse>> _emptyStreamerList = new(capacity: 0);
        private readonly ConcurrentDictionary<long, UserData> _userMap;

        public ClientContainer()
        {
            _userMap = new ConcurrentDictionary<long, UserData>();
        }

        public void AddClient(in long userID, IStreamer<ListenResponse> streamer)
        {
            UserData userData = _userMap.GetOrAdd(userID, _ => new UserData());
            if (!userData.Streamers.Add(streamer))
            {
                throw new InvalidOperationException($"Client for user {userID} has already been added.");
            }
        }

        public void RemoveClient(in long userID, IStreamer<ListenResponse> streamer)
        {
            if (_userMap.TryGetValue(userID, out UserData userData))
            {
                if (!userData.Streamers.TryRemove(streamer))
                {
                    throw new InvalidOperationException($"Client for user {userID} has already been removed.");
                }
            }
        }

        public IEnumerable<IStreamer<ListenResponse>> GetClients(in long userID)
        {
            if (_userMap.TryGetValue(userID, out UserData userData))
            {
                return userData.Streamers;
            }
            else
            {
                return _emptyStreamerList;
            }
        }

        private sealed class UserData
        {
            public ConcurrentHashSet<IStreamer<ListenResponse>> Streamers { get; }
                // we don't usually expect the connected clients to be > 1
                = new(concurrencyLevel: Environment.ProcessorCount, capacity: 2);
        }
    }
}
