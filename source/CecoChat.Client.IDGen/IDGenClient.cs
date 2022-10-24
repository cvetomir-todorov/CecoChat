using System.Diagnostics;
using CecoChat.Contracts.IDGen;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.IDGen;

public interface IIDGenClient : IDisposable
{
    ValueTask<GetIDResult> GetID(CancellationToken ct);
}

public readonly struct GetIDResult
{
    public bool Success => ID > 0;
    public long ID { get; init; }
}

internal sealed class IDGenClient : IIDGenClient
{
    private readonly ILogger _logger;
    private readonly IDGenOptions _options;
    private readonly Contracts.IDGen.IDGen.IDGenClient _client;
    private readonly IIDChannel _idChannel;
    private readonly Timer _invalidateIDsTimer;
    private int _isRefreshing;
    private static readonly int True = 1;
    private static readonly int False = 0;

    public IDGenClient(
        ILogger<IDGenClient> logger,
        IOptions<IDGenOptions> options,
        Contracts.IDGen.IDGen.IDGenClient client,
        IIDChannel idChannel)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _idChannel = idChannel;

        _logger.LogInformation("IDGen address set to {Address}", _options.Address);
        _invalidateIDsTimer = new(
            callback: _ => RefreshIDs(),
            state: null,
            dueTime: TimeSpan.Zero,
            period: _options.RefreshIDsInterval);
    }

    public void Dispose()
    {
        _invalidateIDsTimer.Dispose();
    }

    public async ValueTask<GetIDResult> GetID(CancellationToken ct)
    {
        (bool success, long id) = await _idChannel.TryTakeID(_options.GetIDWaitInterval, ct);
        if (!success)
        {
            _logger.LogWarning("Timed-out while waiting for new IDs to be generated");
            return new GetIDResult();
        }

        return new GetIDResult { ID = id };
    }

    private void RefreshIDs()
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
                OriginatorId = _options.OriginatorID,
                Count = _options.RefreshIDsCount
            };
            DateTime deadline = DateTime.UtcNow.Add(_options.CallTimeout);

            Activity.Current = null;
            _idChannel.ClearIDs();
            GenerateManyResponse response = _client.GenerateMany(request, deadline: deadline);
            _idChannel.AddNewIDs(response.Ids);
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