using System.Threading.Channels;

namespace CecoChat.IdGen.Client;

internal interface IIdChannel
{
    ValueTask<(bool, long)> TryTakeId(TimeSpan timeout, CancellationToken ct);

    void ClearIds();

    void AddNewIds(IReadOnlyCollection<long> newIds);
}

internal sealed class IdChannel : IIdChannel
{
    private readonly Channel<long> _blueChannel;
    private readonly Channel<long> _greenChannel;
    private Channel<long> _currentChannel;

    public IdChannel()
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

    public async ValueTask<(bool, long)> TryTakeId(TimeSpan timeout, CancellationToken ct)
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

    public void ClearIds()
    {
        while (_currentChannel.Reader.TryRead(out long _))
        {
            // drain the current channel
        }
    }

    public void AddNewIds(IReadOnlyCollection<long> newIds)
    {
        Channel<long> otherChannel = _blueChannel != _currentChannel ? _blueChannel : _greenChannel;

        while (otherChannel.Reader.TryRead(out long _))
        {
            // drain the other channel
        }

        foreach (long newId in newIds)
        {
            otherChannel.Writer.TryWrite(newId);
        }

        _currentChannel = otherChannel;
    }
}
