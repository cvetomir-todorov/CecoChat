using System.Diagnostics;
using CecoChat.Contracts.IDGen;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.IdGen;

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
    private readonly IdGenOptions _options;
    private readonly Contracts.IDGen.IDGen.IDGenClient _client;
    private readonly IIdChannel _idChannel;
    private readonly Timer _invalidateIdsTimer;
    private int _isRefreshing;
    private static readonly int True = 1;
    private static readonly int False = 0;

    public IdGenClient(
        ILogger<IdGenClient> logger,
        IOptions<IdGenOptions> options,
        Contracts.IDGen.IDGen.IDGenClient client,
        IIdChannel idChannel)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
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
                OriginatorId = _options.OriginatorId,
                Count = _options.RefreshIdsCount
            };
            DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);

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
