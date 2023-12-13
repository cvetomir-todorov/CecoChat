using CecoChat.Contracts.Config;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Admin.Backplane;

public interface IConfigChangesProducer : IDisposable
{
    void NotifyChanges(string configSection);
}

public sealed class ConfigChangesProducer : IConfigChangesProducer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IKafkaProducer<Null, ConfigChange> _producer;

    public ConfigChangesProducer(
        ILogger<ConfigChangesProducer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IFactory<IKafkaProducer<Null, ConfigChange>> producerFactory)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _producer = producerFactory.Create();

        _producer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ConfigChangesProducer, new ConfigChangeSerializer());
        applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }

    public void NotifyChanges(string configSection)
    {
        ConfigChange configChange = new()
        {
            ConfigSection = configSection,
            When = DateTime.UtcNow.ToTimestamp()
        };
        Message<Null, ConfigChange> kafkaMessage = new()
        {
            Value = configChange
        };

        _producer.Produce(kafkaMessage, _backplaneOptions.ConfigChangesTopic);
        _logger.LogInformation("Notified about changes in config section {ConfigSection}", configSection);
    }
}
