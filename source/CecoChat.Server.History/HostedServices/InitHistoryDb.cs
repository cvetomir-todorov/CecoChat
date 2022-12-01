using CecoChat.Cassandra;
using CecoChat.Data.History;
using CecoChat.Data.History.Repos;

namespace CecoChat.Server.History.HostedServices;

public sealed class InitHistoryDb : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ICassandraDbInitializer _dbInitializer;
    private readonly IChatMessageRepo _messageRepo;
    private readonly HistoryDbInitHealthCheck _historyDbInitHealthCheck;

    public InitHistoryDb(
        ILogger<InitHistoryDb> logger,
        ICassandraDbInitializer dbInitializer,
        IChatMessageRepo messageRepo,
        HistoryDbInitHealthCheck historyDbInitHealthCheck)
    {
        _logger = logger;
        _dbInitializer = dbInitializer;
        _messageRepo = messageRepo;
        _historyDbInitHealthCheck = historyDbInitHealthCheck;
    }

    public void Dispose()
    {
        _messageRepo.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _dbInitializer.Initialize(keyspace: "history", scriptSource: typeof(IHistoryDbContext).Assembly);

        Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Start preparing queries...");
                _messageRepo.Prepare();
                _historyDbInitHealthCheck.IsReady = true;
                _logger.LogInformation("Completed preparing queries");
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failed to prepare queries");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
