using CecoChat.Data.Config;
using CecoChat.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CecoChat.Server.Admin.HostedServices;

public class InitConfigDb : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigDbOptions _configDbOptions;
    private readonly INpgsqlDbInitializer _initializer;
    private readonly ConfigDbContext _configDbContext;

    public InitConfigDb(
        ILogger<InitConfigDb> logger,
        IOptions<ConfigDbOptions> configDbOptions,
        ConfigDbContext configDbContext,
        INpgsqlDbInitializer initializer)
    {
        _logger = logger;
        _configDbOptions = configDbOptions.Value;
        _configDbContext = configDbContext;
        _initializer = initializer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string database = new NpgsqlConnectionStringBuilder(_configDbOptions.Connect.ConnectionString).Database!;
        _initializer.Initialize(_configDbOptions.Init, database, typeof(ConfigDbAutofacModule).Assembly);

        await Seed();
    }

    private async Task Seed()
    {
        ElementEntity[] elements = await _configDbContext.Elements.ToArrayAsync();
        if (elements.Length > 0)
        {
            _logger.LogInformation("{ConfigElementCount} config elements present:", elements.Length);
            LogConfig(elements);
            return;
        }

        (string deploymentEnvironment, elements) = GetSeedValues();
        if (elements.Length == 0)
        {
            _logger.LogCritical("Failed to seed default dynamic config for unknown deployment environment {DeploymentEnvironment}", deploymentEnvironment);
            return;
        }

        DateTime version = DateTime.UtcNow;
        foreach (ElementEntity element in elements)
        {
            element.Version = version;
        }

        _configDbContext.Elements.AddRange(elements);
        await _configDbContext.SaveChangesAsync();
        _configDbContext.ChangeTracker.Clear();

        _logger.LogInformation("Seeded default dynamic config with {ConfigElementCount} values for deployment environment {DeploymentEnvironment}:", elements.Length, deploymentEnvironment);
        LogConfig(elements);
    }

    private void LogConfig(ElementEntity[] elements)
    {
        foreach (ElementEntity element in elements)
        {
            _logger.LogInformation("{ConfigElementName}: {ConfigElementValue}", element.Name, element.Value);
        }
    }

    private (string, ElementEntity[]) GetSeedValues()
    {
        string deploymentEnvironment = _configDbOptions.DeploymentEnvironment;
        if (string.IsNullOrWhiteSpace(deploymentEnvironment))
        {
            _logger.LogWarning("Empty deployment environment, assuming value 'docker'");
            deploymentEnvironment = "docker";
        }

        ElementEntity[] elements;

        if (string.Equals(deploymentEnvironment, "docker", StringComparison.InvariantCultureIgnoreCase))
        {
            elements = new ElementEntity[]
            {
                new() { Name = ConfigKeys.Partitioning.Count, Value = "12" },
                new() { Name = ConfigKeys.Partitioning.Partitions, Value = "0=0-5;1=6-11" },
                new() { Name = ConfigKeys.Partitioning.Addresses, Value = "0=https://localhost:31000;1=https://localhost:31001" },
                new() { Name = ConfigKeys.History.MessageCount, Value = "32" },
                new() { Name = ConfigKeys.Snowflake.GeneratorIds, Value = "0=0,1,2,3" }
            };
        }
        else if (string.Equals(deploymentEnvironment, "minikube", StringComparison.InvariantCultureIgnoreCase))
        {
            elements = new ElementEntity[]
            {
                new() { Name = ConfigKeys.Partitioning.Count, Value = "12" },
                new() { Name = ConfigKeys.Partitioning.Partitions, Value = "0=0-5;1=6-11" },
                new() { Name = ConfigKeys.Partitioning.Addresses, Value = "0=https://messaging.cecochat.com/m0;1=https://messaging.cecochat.com/m1" },
                new() { Name = ConfigKeys.History.MessageCount, Value = "32" },
                new() { Name = ConfigKeys.Snowflake.GeneratorIds, Value = "0=0,1;1=2,3" }
            };
        }
        else
        {
            elements = Array.Empty<ElementEntity>();
        }

        return (deploymentEnvironment, elements);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
