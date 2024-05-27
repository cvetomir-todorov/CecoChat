using System.Net;
using System.Net.Sockets;
using Cassandra;
using Common;
using Common.Cassandra;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CecoChat.Chats.Testing;

public class TestContainers
{
    private INetwork _network;
    private IContainer _cassandra;
    private int _cassandraHostPort;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _network = new NetworkBuilder()
            .WithName("cecochat")
            .Build();

        _cassandraHostPort = NetworkTools.GetNextFreeTcpPort();
        await TestContext.Progress.WriteLineAsync($"Setting Cassandra host port to {_cassandraHostPort}");
        const int cassandraContainerPort = 9042;
        const string localDc = "Europe";

        _cassandra = new ContainerBuilder()
            .WithImage("cassandra:4.1.3")
            .WithName("cecochat-test-cassandra0")
            .WithHostname("cassandra0")
            .WithNetwork(_network)
            .WithPortBinding(_cassandraHostPort, cassandraContainerPort)
            .WithEnvironment(new Dictionary<string, string>
            {
                { "CASSANDRA_SEEDS", "cassandra0" },
                { "CASSANDRA_CLUSTER_NAME", "cecochat" },
                { "CASSANDRA_DC", localDc },
                { "CASSANDRA_RACK", "Rack0" },
                { "CASSANDRA_ENDPOINT_SNITCH", "GossipingPropertyFileSnitch" },
                { "CASSANDRA_NUM_TOKENS", "128" },
                { "HEAP_NEWSIZE", "128M" },
                { "MAX_HEAP_SIZE", "512M" }
            })
            .WithWaitStrategy(Wait.ForUnixContainer()
                //.UntilMessageIsLogged($"Starting listening for CQL clients on /0.0.0.0:{port}")
                .UntilCassandraQueryExecuted(_cassandraHostPort, localDc))
            .WithLogger(new NUnitProgressLogger<TestContainers>())
            .Build();

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromMinutes(5));
        await _cassandra.StartAsync(timeoutCts.Token);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        (string stdout, string stderr) logs = await _cassandra.GetLogsAsync();
        await TestContext.Progress.WriteLineAsync("Stdout ===========================================");
        await TestContext.Progress.WriteLineAsync(logs.stdout);
        await TestContext.Progress.WriteLineAsync("Stderr ===========================================");
        await TestContext.Progress.WriteLineAsync(logs.stderr);

        await _cassandra.DisposeAsync();
        await _network.DisposeAsync();
    }

    [Test]
    public void Test1()
    {
        CassandraOptions options = new()
        {
            ContactPoints = [$"{_cassandra.Hostname}:{_cassandraHostPort}"],
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

public class NUnitProgressLogger<TCategoryName> : ILogger<TCategoryName>
{
#pragma warning disable IDE0060
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
#pragma warning restore IDE0060

#pragma warning disable IDE0060
    public bool IsEnabled(LogLevel logLevel) => true;
#pragma warning restore IDE0060

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter.Invoke(state, exception);
        string level;
        switch (logLevel)
        {
            case LogLevel.Trace:
                level = "VRB";
                break;
            case LogLevel.Debug:
                level = "DBG";
                break;
            case LogLevel.Information:
                level = "INF";
                break;
            case LogLevel.Warning:
                level = "WRN";
                break;
            case LogLevel.Error:
                level = "ERR";
                break;
            case LogLevel.Critical:
                level = "CRI";
                break;
            default:
                throw new EnumValueNotSupportedException(logLevel);
        }

        TestContext.Progress.WriteLine("[{0:HH:mm:ss.fff} {1}] {2} {3}", DateTime.UtcNow, level, message, exception);
    }
}

public static class WaitForContainerOsExtensions
{
    public static IWaitForContainerOS UntilCassandraQueryExecuted(this IWaitForContainerOS waitFor, int port, string localDc)
    {
        return waitFor.AddCustomWaitStrategy(new CassandraQueryExecutedWaitStrategy(port, localDc));
    }
}

public class CassandraQueryExecutedWaitStrategy : IWaitUntil
{
    private readonly int _port;
    private readonly string _localDc;
    private readonly TimeSpan _showErrorInterval;
    private DateTime _lastShowError;

    public CassandraQueryExecutedWaitStrategy(int port, string localDc)
    {
        _port = port;
        _localDc = localDc;
        _showErrorInterval = TimeSpan.FromSeconds(10);
    }

    public Task<bool> UntilAsync(IContainer container)
    {
        CassandraOptions options = new()
        {
            ContactPoints = [$"{container.Hostname}:{_port}"],
            LocalDc = _localDc,
            SocketConnectTimeout = TimeSpan.FromSeconds(5),
            ExponentialReconnectPolicy = true,
            ExponentialReconnectPolicyBaseDelay = TimeSpan.FromMilliseconds(500),
            ExponentialReconnectPolicyMaxDelay = TimeSpan.FromSeconds(5)
        };

        CassandraDbContext? db = null;

        try
        {
            db = new(new NUnitProgressLogger<CassandraDbContext>(), new OptionsWrapper<CassandraOptions>(options));
            // we simply want to make sure that there is connection to the cassandra cluster
            // we don't care if this keyspace actually exists
            db.ExistsKeyspace("non-existing");

            return Task.FromResult(true);
        }
        catch (NoHostAvailableException noHostAvailableException)
        {
            DateTime currentShowError = DateTime.UtcNow;
            TimeSpan timeSinceLastShowError = currentShowError - _lastShowError;

            if (timeSinceLastShowError > _showErrorInterval)
            {
                container.Logger.LogError(
                    noHostAvailableException,
                    "No connection to Cassandra at {Host}:{Port} (showing this error once each {ShowErrorInterval} secs)",
                    container.Hostname, _port, Math.Ceiling(_showErrorInterval.TotalSeconds));
                _lastShowError = currentShowError;
            }

            return Task.FromResult(false);
        }
        finally
        {
            db?.Dispose();
        }
    }
}

public static class NetworkTools
{
    public static int GetNextFreeTcpPort()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // specifying value of 0 makes the OS to choose the next free port
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;

        return port;
    }
}
