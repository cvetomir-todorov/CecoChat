using CecoChat.Config.Contracts;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Config.Backplane;

public interface IConfigChangesConsumer : IDisposable
{
    void Prepare();

    void Start(CancellationToken ct);
}

internal sealed class ConfigChangesConsumer : IConfigChangesConsumer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _options;
    private readonly IKafkaConsumer<Null, ConfigChange> _consumer;
    private readonly IEnumerable<IConfigChangeSubscriber> _configChangeSubscribers;

    public ConfigChangesConsumer(
        ILogger<ConfigChangesConsumer> logger,
        IOptions<BackplaneOptions> options,
        IFactory<IKafkaConsumer<Null, ConfigChange>> consumerFactory,
        IEnumerable<IConfigChangeSubscriber> configChangeSubscribers)
    {
        _logger = logger;
        _options = options.Value;
        _consumer = consumerFactory.Create();
        _configChangeSubscribers = configChangeSubscribers;
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }

    public void Prepare()
    {
        _options.ConfigChangesConsumer.ConsumerGroupId = $"{_options.ConfigChangesConsumer.ConsumerGroupId}-{Environment.MachineName}";
        _logger.LogInformation("Use {ConfigChangesConsumerGroup} for config changes consumer group", _options.ConfigChangesConsumer.ConsumerGroupId);

        _consumer.Initialize(_options.Kafka, _options.ConfigChangesConsumer, new ConfigChangeDeserializer());
        _consumer.Subscribe(_options.TopicConfigChanges);
    }

    public void Start(CancellationToken ct)
    {
        _logger.LogInformation("Start processing config changes");

        while (!ct.IsCancellationRequested)
        {
            _consumer.Consume(consumeResult =>
            {
                ProcessMessage(consumeResult.Message.Value, ct);
            }, ct);
        }

        _logger.LogInformation("Stopped processing config changes");
    }

    private void ProcessMessage(ConfigChange configChange, CancellationToken ct)
    {
        TimeSpan delta = TimeSpan.FromSeconds(1);

        foreach (IConfigChangeSubscriber subscriber in _configChangeSubscribers)
        {
            if (!string.Equals(subscriber.ConfigSection, configChange.ConfigSection, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }
            if (configChange.When.ToDateTime() < subscriber.ConfigVersion + delta)
            {
                _logger.LogInformation("Skip config change for config section {ConfigSection} dated at {ConfigChangeDateTime} because current config values are newer {CurrentConfigDateTime}",
                    configChange.ConfigSection, configChange.When.ToDateTime(), subscriber.ConfigVersion);
                continue;
            }

            subscriber.NotifyConfigChange(ct).ContinueWith(task =>
            {
                _logger.LogError(task.Exception, "Failed processing for config change for config section {ConfigSection}", configChange.ConfigSection);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
