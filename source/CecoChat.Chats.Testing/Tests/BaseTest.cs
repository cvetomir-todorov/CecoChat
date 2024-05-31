using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public abstract class BaseTest
{
    private INetwork _dockerNetwork;
    private ChatsDb _chatsDb;
    private bool _chatsDbStarted;

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        _dockerNetwork = new NetworkBuilder()
            .WithName("cecochat")
            .Build();

        _chatsDb = new(_dockerNetwork, name: "cassandra0", cluster: "cecochat", seeds: "cassandra0", localDc: "Europe");
        await _chatsDb.Start(TimeSpan.FromMinutes(5));
        _chatsDbStarted = true;
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        if (!_chatsDbStarted)
        {
            await _chatsDb.PrintLogs();
        }

        await _chatsDb.DisposeAsync();
        await _dockerNetwork.DisposeAsync();
    }
}
