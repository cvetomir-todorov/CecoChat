using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        void AddClient(in int userID, IStreamer<ListenResponse> streamer);

        void RemoveClient(in int userID, IStreamer<ListenResponse> streamer);

        IReadOnlyCollection<IStreamer<ListenResponse>> GetClients(in int userID);
    }

    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamer<ListenResponse>> _emptyStreamerList = new(capacity: 0);
        private readonly ConcurrentDictionary<int, UserData> _userMap;

        public ClientContainer()
        {
            _userMap = new ConcurrentDictionary<int, UserData>();
        }

        public void AddClient(in int userID, IStreamer<ListenResponse> streamer)
        {
            UserData userData = _userMap.GetOrAdd(userID, _ => new UserData());
            // TODO: figure out how to avoid same client being added twice
            userData.StreamerList.Add(streamer);
        }

        public void RemoveClient(in int userID, IStreamer<ListenResponse> streamer)
        {
            if (_userMap.TryGetValue(userID, out UserData userData))
            {
                userData.StreamerList.Remove(streamer);
            }
        }

        public IReadOnlyCollection<IStreamer<ListenResponse>> GetClients(in int userID)
        {
            if (_userMap.TryGetValue(userID, out UserData userData))
            {
                return userData.StreamerList;
            }
            else
            {
                return _emptyStreamerList;
            }
        }

        private sealed class UserData
        {
            public List<IStreamer<ListenResponse>> StreamerList { get; } = new();
        }
    }
}
