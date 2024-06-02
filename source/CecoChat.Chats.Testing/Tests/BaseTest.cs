using CecoChat.Testing;
using Common.Jwt;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public abstract class BaseTest
{
    private INetwork _dockerNetwork;
    private ChatsDb _chatsDb;
    private bool _chatsDbStarted;
    private ChatsService _chatsService;
    private ChatsClient _chatsClient;

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        _dockerNetwork = new NetworkBuilder()
            .WithName("cecochat")
            .Build();

        _chatsDb = new(_dockerNetwork, name: "cassandra0", cluster: "cecochat", seeds: "cassandra0", localDc: "Europe");
        await _chatsDb.Start(TimeSpan.FromMinutes(5));
        _chatsDbStarted = true;

        _chatsService = new ChatsService(
            environment: "Test",
            listenPort: 32004,
            certificatePath: "services.pfx",
            certificatePassword: "cecochat",
            configFilePath: "chats-service-settings.json",
            _chatsDb);
        await _chatsService.Run();

        _chatsClient = new(
            configFilePath: "chats-client-settings.json");
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        _chatsClient.Dispose();
        await _chatsService.DisposeAsync();

        if (!_chatsDbStarted)
        {
            await _chatsDb.PrintLogs();
        }
        await _chatsDb.DisposeAsync();
        await _dockerNetwork.DisposeAsync();
    }

    public ChatsClient Client => _chatsClient;

    protected string CreateUserAccessToken(long userId, string userName)
    {
        JwtOptions jwtOptions = _chatsService.GetJwtOptions();
        string accessToken = Auth.CreateUserAccessToken(userId, userName, jwtOptions);

        return accessToken;
    }
}
