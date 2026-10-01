using CecoChat.Testing;
using Common.Jwt;
using Common.Testing.AspNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public abstract class BaseTest
{
    private INetwork _dockerNetwork;
    private IChatsDb? _chatsDb;
    private bool _chatsDbStarted;
    private ChatsService _chatsService;
    private ChatsClient _chatsClient;

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        _dockerNetwork = new NetworkBuilder()
            .WithName("cecochat-test")
            .Build();

        string? startChatsDbEnvVar = Environment.GetEnvironmentVariable("CECOCHAT_START_TEST_CONTAINERS_CHATS_DB");
        if (string.Equals(startChatsDbEnvVar, "true", StringComparison.OrdinalIgnoreCase))
        {
            _chatsDb = new TestContainersChatsDb(_dockerNetwork, name: "cassandra0", cluster: "cecochat", seeds: "cassandra0", localDc: "Europe");
            await _chatsDb.Start(TimeSpan.FromMinutes(5));
            _chatsDbStarted = true;
        }
        else
        {
            _chatsDb = new ExistingChatsDb();
        }

        ServiceOptions options = new()
        {
            Environment = "Test",
            ListenPort = 32004,
            CertificatePath = "services.pfx",
            CertificatePassword = "cecochat",
            ConfigFilePath = "chats-service.json"
        };
        _chatsService = new ChatsService(options, _chatsDb);
        await _chatsService.Start();

        _chatsClient = new(configFilePath: "chats-client.json");

        await CleanTestData();
        await AddTestData();
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        await CleanTestData();

        _chatsClient.Dispose();
        await _chatsService.DisposeAsync();

        if (_chatsDb != null)
        {
            if (!_chatsDbStarted)
            {
                await _chatsDb.PrintLogs();
            }
            await _chatsDb.DisposeAsync();
        }
        await _dockerNetwork.DisposeAsync();
    }

    protected virtual Task CleanTestData() => Task.CompletedTask;

    protected virtual Task AddTestData() => Task.CompletedTask;

    protected ChatsClient Client => _chatsClient;

    protected ChatsService Service => _chatsService;

    protected string CreateUserAccessToken(long userId, string userName)
    {
        JwtOptions jwtOptions = _chatsService.GetJwtOptions();
        string accessToken = Auth.CreateUserAccessToken(userId, userName, jwtOptions);

        return accessToken;
    }
}
