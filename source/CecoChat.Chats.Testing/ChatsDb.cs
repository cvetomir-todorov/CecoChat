using Common.Net;
using Common.Testing.Logging;
using Common.Testing.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace CecoChat.Chats.Testing;

public sealed class ChatsDb : IAsyncDisposable
{
    private readonly int _cassandraHostPort;
    private readonly IContainer _cassandra;

    public ChatsDb(INetwork dockerNetwork, string name, string cluster, string seeds, string localDc)
    {
        _cassandraHostPort = NetworkUtils.GetNextFreeTcpPort();
        TestContext.Progress.WriteLine($"Setting Cassandra host port to {_cassandraHostPort}");

        const int cassandraContainerPort = 9042;

        _cassandra = new ContainerBuilder()
            .WithImage("cassandra:4.1.3")
            .WithName($"cecochat-test-{name}")
            .WithHostname(name)
            .WithNetwork(dockerNetwork)
            .WithPortBinding(_cassandraHostPort, cassandraContainerPort)
            .WithEnvironment(new Dictionary<string, string>
            {
                { "CASSANDRA_SEEDS", seeds },
                { "CASSANDRA_CLUSTER_NAME", cluster },
                { "CASSANDRA_DC", localDc },
                { "CASSANDRA_RACK", "Rack0" },
                { "CASSANDRA_ENDPOINT_SNITCH", "GossipingPropertyFileSnitch" },
                { "CASSANDRA_NUM_TOKENS", "128" },
                { "HEAP_NEWSIZE", "128M" },
                { "MAX_HEAP_SIZE", "512M" }
            })
            .WithWaitStrategy(Wait.ForUnixContainer()
                //.UntilMessageIsLogged($"Starting listening for CQL clients on /0.0.0.0:{port}")
                .UntilCassandraQueryExecuted(_cassandraHostPort, localDc, showErrorInterval: TimeSpan.FromSeconds(10)))
            .WithLogger(new NUnitProgressLogger<TestContainers>())
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _cassandra.DisposeAsync();
    }

    public string Host => _cassandra.Hostname;

    public int Port => _cassandraHostPort;

    public async Task Start(TimeSpan timeout)
    {
        using CancellationTokenSource timeoutCts = new(timeout);
        await _cassandra.StartAsync(timeoutCts.Token);
    }

    public async Task PrintLogs()
    {
        (string stdout, string stderr) logs = await _cassandra.GetLogsAsync();

        await TestContext.Progress.WriteLineAsync($"{_cassandra.Hostname} stdout:");
        await TestContext.Progress.WriteLineAsync(logs.stdout);
        await TestContext.Progress.WriteLineAsync($"{_cassandra.Hostname} stderr:");
        await TestContext.Progress.WriteLineAsync(logs.stderr);
    }
}
