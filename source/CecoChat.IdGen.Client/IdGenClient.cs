using System.Diagnostics;
using CecoChat.IdGen.Contracts;
using Common;
using Grpc.Core;
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
    private readonly IIdChannel _idChannel;
    private readonly Timer _invalidateIdsTimer;
    private int _isRefreshing;
    private static readonly int True = 1;
    private static readonly int False = 0;

    public IdGenClient(
        ILogger<IdGenClient> logger,
        IOptions<IdGenClientOptions> options,
        CecoChat.IdGen.Contracts.IdGen.IdGenClient client,
        IClock clock,
        IIdChannel idChannel)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _clock = clock;
        _idChannel = idChannel;

        _logger.LogInformation("ID Gen address set to {Address}", _options.Address);
        _logger.LogInformation("Start refreshing message IDs each {RefreshIdsInterval:##} ms with {RefreshIdsCount} IDs",
            _options.RefreshIdsInterval.TotalMilliseconds, _options.RefreshIdsCount);
        _invalidateIdsTimer = new(
            callback: _ => RefreshIds(),
            state: null,
            dueTime: TimeSpan.Zero,
            period: _options.RefreshIdsInterval);
    }

    public void Dispose()
    {
        _invalidateIdsTimer.Dispose();
    }

    public async ValueTask<GetIdResult> GetId(CancellationToken ct)
    {
        (bool success, long id) = await _idChannel.TryTakeId(_options.GetIdWaitInterval, ct);
        if (!success)
        {
            _logger.LogWarning("Timed-out while waiting for new IDs to be generated");
            return new GetIdResult();
        }

        return new GetIdResult { Id = id };
    }

    private void RefreshIds()
    {
        if (True == Interlocked.CompareExchange(ref _isRefreshing, True, False))
        {
            _logger.LogWarning("Failed to refresh IDs since previous refresh hasn't completed yet");
            return;
        }

        try
        {
            GenerateManyRequest request = new()
            {
                Count = _options.RefreshIdsCount
            };
            DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);

            Activity.Current = null;
            _idChannel.ClearIds();
            GenerateManyResponse response = _client.GenerateMany(request, deadline: deadline);
            _idChannel.AddNewIds(response.Ids);
        }
        catch (RpcException rpcException)
        {
            _logger.LogError(rpcException, "Failed to refresh IDs due to error {Error}", rpcException.Status);
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
}
