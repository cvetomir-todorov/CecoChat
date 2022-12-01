using CecoChat.Cassandra;
using CecoChat.Data.State;
using CecoChat.Data.State.Repos;

namespace CecoChat.Server.State.HostedServices;

public sealed class InitStateDb : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ICassandraDbInitializer _dbInitializer;
    private readonly IChatStateRepo _chatStateRepo;
    private readonly StateDbInitHealthCheck _stateDbInitHealthCheck;

    public InitStateDb(
        ILogger<InitStateDb> logger,
        ICassandraDbInitializer dbInitializer,
        IChatStateRepo chatStateRepo,
        StateDbInitHealthCheck stateDbInitHealthCheck)
    {
        _logger = logger;
        _dbInitializer = dbInitializer;
        _chatStateRepo = chatStateRepo;
        _stateDbInitHealthCheck = stateDbInitHealthCheck;
    }

    public void Dispose()
    {
        _chatStateRepo.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _dbInitializer.Initialize(keyspace: "state", scriptSource: typeof(IStateDbContext).Assembly);

        Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Start preparing queries...");
                _chatStateRepo.Prepare();
                _stateDbInitHealthCheck.IsReady = true;
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
