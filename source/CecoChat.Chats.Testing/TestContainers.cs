using Common.Cassandra;
using Common.Testing.Logging;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CecoChat.Chats.Testing;

public class TestContainers
{
    private INetwork _network;
    private ChatsDb _chatsDb;
    private bool _chatsDbStarted;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _network = new NetworkBuilder()
            .WithName("cecochat")
            .Build();

        _chatsDb = new(_network, name: "cassandra0", cluster: "cecochat", seeds: "cassandra0", localDc: "Europe");
        await _chatsDb.Start(TimeSpan.FromMinutes(5));
        _chatsDbStarted = true;
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (!_chatsDbStarted)
        {
            await _chatsDb.PrintLogs();
        }

        await _chatsDb.DisposeAsync();
        await _network.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
        CassandraOptions options = new()
        {
            ContactPoints = [$"{_chatsDb.Host}:{_chatsDb.Port}"],
            LocalDc = "Europe",
            SocketConnectTimeout = TimeSpan.FromSeconds(5),
            ExponentialReconnectPolicy = true,
            ExponentialReconnectPolicyBaseDelay = TimeSpan.FromMilliseconds(500),
            ExponentialReconnectPolicyMaxDelay = TimeSpan.FromSeconds(5)
        };

        using CassandraDbContext db = new(new NUnitProgressLogger<CassandraDbContext>(), new OptionsWrapper<CassandraOptions>(options));
        TestContext.Progress.WriteLine("Exists keyspace? {0}", db.ExistsKeyspace("unknown"));
    }
}
