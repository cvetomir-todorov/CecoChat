using System.Threading.Channels;

namespace CecoChat.Client.IdGen;

internal interface IIDChannel
{
    ValueTask<(bool, long)> TryTakeID(TimeSpan timeout, CancellationToken ct);

    void ClearIDs();

    void AddNewIDs(IReadOnlyCollection<long> newIDs);
}

internal sealed class IDChannel : IIDChannel
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
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;

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
        while (_currentChannel.Reader.TryRead(out long _))
        {
            // drain the current channel
        }
    }

    public void AddNewIDs(IReadOnlyCollection<long> newIDs)
    {
        Channel<long> otherChannel = _blueChannel != _currentChannel ? _blueChannel : _greenChannel;

        while (otherChannel.Reader.TryRead(out long _))
        {
            // drain the other channel
        }

        foreach (long newID in newIDs)
        {
            otherChannel.Writer.TryWrite(newID);
        }

        _currentChannel = otherChannel;
    }
}
