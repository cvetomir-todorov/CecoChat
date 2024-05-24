using System.Threading.Channels;
using CecoChat.IdGen.Contracts;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.IdGen.Client;

public interface IIdGenClient : IDisposable
{
    ValueTask<GetIdResult> GetId(CancellationToken ct);
}

public readonly struct GetIdResult
{
    public bool Success => Id > 0;
    public long Id { get; init; }
}

internal sealed class IdGenClient : IIdGenClient
{
    private readonly ILogger _logger;
    private readonly IdGenClientOptions _options;
    private readonly CecoChat.IdGen.Contracts.IdGen.IdGenClient _client;
    private readonly IClock _clock;
    private readonly Channel<long> _channel;
    private readonly Timer _refreshIdsTimer;
    private int _isRefreshing;
    private static readonly int True = 1;
    private static readonly int False = 0;

    public IdGenClient(
        ILogger<IdGenClient> logger,
        IOptions<IdGenClientOptions> options,
        CecoChat.IdGen.Contracts.IdGen.IdGenClient client,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _clock = clock;

        UnboundedChannelOptions channelOptions = new()
        {
            SingleReader = false,
            SingleWriter = true
        };
        _channel = Channel.CreateUnbounded<long>(channelOptions);

        _refreshIdsTimer = new Timer(
            callback: RefreshIds,
            state: null,
            dueTime: TimeSpan.Zero,
            period: _options.RefreshIdsInterval);

        _logger.LogInformation("ID Gen address set to {Address}", _options.Address);
        _logger.LogInformation("Start refreshing message IDs each {RefreshIdsInterval:##} ms with {RefreshIdsCount} IDs",
            _options.RefreshIdsInterval.TotalMilliseconds, _options.RefreshIdsCount);
    }

    public void Dispose()
    {
        _refreshIdsTimer.Dispose();
        _channel.Writer.Complete();
    }

    public async ValueTask<GetIdResult> GetId(CancellationToken ct)
    {
        if (_channel.Reader.TryRead(out long quicklyReadId))
        {
            return new GetIdResult
            {
                Id = quicklyReadId
            };
        }
        else
        {
            // channel is empty
            TryAsynchronousRefreshOnDemand();
        }

        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;

        try
        {
            timeoutCts = new CancellationTokenSource(_options.GetIdWaitInterval);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);

            long id = await _channel.Reader.ReadAsync(linkedCts.Token);
            return new GetIdResult
            {
                Id = id
            };
        }
        catch (OperationCanceledException)
        {
            ct.ThrowIfCancellationRequested();
            return new GetIdResult();
        }
        finally
        {
            timeoutCts?.Dispose();
            linkedCts?.Dispose();
        }
    }

    private void TryAsynchronousRefreshOnDemand()
    {
        if (_isRefreshing == False)
        {
            Task.Run(() => RefreshIds(null));
        }
    }

    /// <summary>
    /// Drains the existing IDs while simultaneously requesting new ones.
    /// Populates the channel with the successfully received new IDs.
    /// </summary>
    private async void RefreshIds(object? state)
    {
        if (True == Interlocked.CompareExchange(ref _isRefreshing, True, False))
        {
            _logger.LogTrace("Failed to refresh IDs since previous refresh hasn't completed yet");
            return;
        }

        try
        {
            await DoRefreshIds();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to refresh IDs");
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshing, False);
        }
    }

    private async Task DoRefreshIds()
    {
        Task drainTask = DrainChannel();
        Task<ICollection<long>> getIdsTask = GetIds();

        Task.WaitAll(drainTask, getIdsTask);

        ICollection<long> newIds = await getIdsTask;
        foreach (long newId in newIds)
        {
            _channel.Writer.TryWrite(newId);
        }

        _logger.LogTrace("Refreshed IDs by {IdCount}", newIds.Count);
    }

    private Task DrainChannel()
    {
        while (_channel.Reader.TryRead(out _))
        {
            // just drain the channel
        }

        return Task.CompletedTask;
    }

    private async Task<ICollection<long>> GetIds()
    {
        GenerateManyRequest request = new()
        {
            Count = _options.RefreshIdsCount
        };
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);

        GenerateManyResponse response = await _client.GenerateManyAsync(request, deadline: deadline);
        return response.Ids;
    }
}
