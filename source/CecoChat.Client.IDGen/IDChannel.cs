using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace CecoChat.Client.IDGen
{
    // TODO: make internal and register in an AutoFac module
    public interface IIDChannel
    {
        bool TryTakeID(out long id, TimeSpan timeout, CancellationToken ct);

        void ClearIDs();

        void AddNewIDs(IReadOnlyCollection<long> newIDs);
    }

    public sealed class IDChannel : IIDChannel
    {
        private readonly BlockingCollection<long> _blueChannel;
        private readonly BlockingCollection<long> _greenChannel;
        private BlockingCollection<long> _currentChannel;

        public IDChannel()
        {
            _blueChannel = new();
            _greenChannel = new();
            _currentChannel = _blueChannel;
        }

        public bool TryTakeID(out long id, TimeSpan timeout, CancellationToken ct)
        {
            int timeoutMs = (int)timeout.TotalMilliseconds;
            return _currentChannel.TryTake(out id, timeoutMs, ct);
        }

        public void ClearIDs()
        {
            // drain the current channel
            while (_currentChannel.TryTake(out long _, millisecondsTimeout: 0))
            {}
        }

        public void AddNewIDs(IReadOnlyCollection<long> newIDs)
        {
            BlockingCollection<long> otherChannel = _blueChannel != _currentChannel ? _blueChannel : _greenChannel;

            // drain the other channel
            while (otherChannel.TryTake(out long _, millisecondsTimeout: 0))
            {}

            foreach (long newID in newIDs)
            {
                otherChannel.Add(newID);
            }

            _currentChannel = otherChannel;
        }
    }
}