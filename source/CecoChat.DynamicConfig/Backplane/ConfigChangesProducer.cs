using CecoChat.Config.Contracts;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.DynamicConfig.Backplane;

public interface IConfigChangesProducer : IDisposable
{
    void NotifyChanges(string configSection);
}

internal sealed class ConfigChangesProducer : IConfigChangesProducer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _options;
    private readonly IKafkaProducer<Null, ConfigChange> _producer;

    public ConfigChangesProducer(
        ILogger<ConfigChangesProducer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IFactory<IKafkaProducer<Null, ConfigChange>> producerFactory)
    {
        _logger = logger;
        _options = backplaneOptions.Value;
        _producer = producerFactory.Create();

        _producer.Initialize(_options.Kafka, _options.ConfigChangesProducer, new ConfigChangeSerializer());
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

        _producer.Produce(kafkaMessage, _options.TopicConfigChanges);
        _logger.LogInformation("Notified about changes in config section {ConfigSection}", configSection);
    }
}
