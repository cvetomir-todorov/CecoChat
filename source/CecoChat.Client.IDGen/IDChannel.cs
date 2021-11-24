using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CecoChat.Client.IDGen
{
    // TODO: make internal and register in an AutoFac module
    public interface IIDChannel
    {
        ValueTask<(bool, long)> TryTakeID(TimeSpan timeout, CancellationToken ct);

        void ClearIDs();

        void AddNewIDs(IReadOnlyCollection<long> newIDs);
    }

    public sealed class IDChannel : IIDChannel
    {
        private readonly Channel<long> _blueChannel;
        private readonly Channel<long> _greenChannel;
        private Channel<long> _currentChannel;

        public IDChannel()
        {
            UnboundedChannelOptions options = new()
            {
                SingleReader = false,
                SingleWriter = true
            };
            _blueChannel = Channel.CreateUnbounded<long>(options);
            _greenChannel = Channel.CreateUnbounded<long>(options);
            _currentChannel = _blueChannel;
        }

        public async ValueTask<(bool, long)> TryTakeID(TimeSpan timeout, CancellationToken ct)
        {
            CancellationTokenSource timeoutCts = null;
            CancellationTokenSource linkedCts = null;

            try
            {
                timeoutCts = new CancellationTokenSource(timeout);
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
                long id = await _currentChannel.Reader.ReadAsync(linkedCts.Token);
                return (true, id);
            }
            catch (OperationCanceledException)
            {
                ct.ThrowIfCancellationRequested();
                return (false, 0);
            }
            finally
            {
                timeoutCts?.Dispose();
                linkedCts?.Dispose();
            }
        }

        public void ClearIDs()
        {
            // drain the current channel
            while (_currentChannel.Reader.TryRead(out long _))
            {}
        }

        public void AddNewIDs(IReadOnlyCollection<long> newIDs)
        {
            Channel<long> otherChannel = _blueChannel != _currentChannel ? _blueChannel : _greenChannel;

            // drain the other channel
            while (otherChannel.Reader.TryRead(out long _))
            {}

            foreach (long newID in newIDs)
            {
                otherChannel.Writer.TryWrite(newID);
            }

            _currentChannel = otherChannel;
        }
    }
}