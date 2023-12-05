using CecoChat.Cassandra;
using CecoChat.Data.Chats;
using CecoChat.Data.Chats.ChatMessages;
using CecoChat.Data.Chats.UserChats;

namespace CecoChat.Server.Chats.HostedServices;

public sealed class InitChatsDb : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ICassandraDbInitializer _dbInitializer;
    private readonly IChatMessageRepo _chatMessageRepo;
    private readonly IUserChatsRepo _userChatsRepo;
    private readonly ChatsDbInitHealthCheck _chatsDbInitHealthCheck;

    public InitChatsDb(
        ILogger<InitChatsDb> logger,
        ICassandraDbInitializer dbInitializer,
        IChatMessageRepo chatMessageRepo,
        IUserChatsRepo userChatsRepo,
        ChatsDbInitHealthCheck chatsDbInitHealthCheck)
    {
        _logger = logger;
        _dbInitializer = dbInitializer;
        _chatMessageRepo = chatMessageRepo;
        _userChatsRepo = userChatsRepo;
        _chatsDbInitHealthCheck = chatsDbInitHealthCheck;
    }

    public void Dispose()
    {
        _chatMessageRepo.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _dbInitializer.Initialize(keyspace: "chats", scriptSource: typeof(IChatsDbContext).Assembly);

        Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Start preparing queries...");
                _chatMessageRepo.Prepare();
                _userChatsRepo.Prepare();
                _chatsDbInitHealthCheck.IsReady = true;
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
