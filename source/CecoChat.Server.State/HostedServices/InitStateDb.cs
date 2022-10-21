using CecoChat.Cassandra;
using CecoChat.Data.State;
using CecoChat.Data.State.Repos;

namespace CecoChat.Server.State.HostedServices;

public sealed class InitStateDb : IHostedService
{
    private readonly ILogger _logger;
    private readonly ICassandraDbInitializer _dbInitializer;
    private readonly IChatStateRepo _chatStateRepo;

    public InitStateDb(
        ILogger<InitStateDb> logger,
        ICassandraDbInitializer dbInitializer,
        IChatStateRepo chatStateRepo)
    {
        _logger = logger;
        _dbInitializer = dbInitializer;
        _chatStateRepo = chatStateRepo;
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