using CecoChat.Chats.Data;
using CecoChat.Chats.Data.Entities.ChatMessages;
using CecoChat.Chats.Data.Entities.UserChats;
using Common.AspNet.Init;
using Common.Cassandra;

namespace CecoChat.Server.Chats.Init;

public sealed class ChatsDbInit : InitStep
{
    private readonly ILogger _logger;
    private readonly ICassandraDbInitializer _dbInitializer;
    private readonly IChatMessageRepo _chatMessageRepo;
    private readonly IUserChatsRepo _userChatsRepo;
    private readonly ChatsDbInitHealthCheck _chatsDbInitHealthCheck;

    public ChatsDbInit(
        ILogger<ChatsDbInit> logger,
        ICassandraDbInitializer dbInitializer,
        IChatMessageRepo chatMessageRepo,
        IUserChatsRepo userChatsRepo,
        ChatsDbInitHealthCheck chatsDbInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _dbInitializer = dbInitializer;
        _chatMessageRepo = chatMessageRepo;
        _userChatsRepo = userChatsRepo;
        _chatsDbInitHealthCheck = chatsDbInitHealthCheck;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _chatMessageRepo.Dispose();
            _userChatsRepo.Dispose();
        }
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        Task<bool> success = Task.FromResult(false);

        try
        {
            _dbInitializer.Initialize(keyspace: "chats", scriptSource: typeof(IChatsDbContext).Assembly);

            _logger.LogInformation("Start preparing queries...");

            _chatMessageRepo.Prepare();
            _userChatsRepo.Prepare();
            _chatsDbInitHealthCheck.IsReady = true;

            _logger.LogInformation("Completed preparing queries");

            success = Task.FromResult(true);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Failed to prepare queries");
        }

        return success;
    }
}
