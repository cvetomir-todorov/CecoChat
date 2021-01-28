using System;
using System.Threading;
using CecoChat.Contracts.Backend;
using CecoChat.Server.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Materialize.Server.Backend
{
    // TODO: figure out how to reuse code with messaging server
    public sealed class KafkaConsumer : IBackendConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _options;
        private readonly IConsumer<Null, BackendMessage> _consumer;
        private readonly IProcessor _processor;

        public KafkaConsumer(
            ILogger<KafkaConsumer> logger,
            IOptions<BackendOptions> options,
            IProcessor processor)
        {
            _logger = logger;
            _options = options.Value;
            _processor = processor;

            ConsumerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', _options.BootstrapServers),
                GroupId = _options.ConsumerGroupID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = false,
                AllowAutoCreateTopics = false,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Null, BackendMessage>(configuration)
                .SetValueDeserializer(new KafkaBackendMessageDeserializer())
                .Build();
        }

        public void Prepare()
        {
            _consumer.Subscribe(_options.MessagesTopicName);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start backend consumption.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<Null, BackendMessage> consumeResult = _consumer.Consume(ct);
                    ProcessMessage(consumeResult.Message.Value);
                    _consumer.Commit(consumeResult);
                }
                catch (AccessViolationException accessViolationException)
                {
                    HandleConsumerDisposal(accessViolationException, ct);
                }
                catch (ObjectDisposedException objectDisposedException)
                {
                    HandleConsumerDisposal(objectDisposedException, ct);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error during backend consumption.");
                }
            }

            _logger.LogInformation("Stopped backend consumption.");
        }

        private void ProcessMessage(BackendMessage message)
        {
            _processor.Process(message);
        }

        private void HandleConsumerDisposal(Exception exception, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogError(exception, "Kafka consumer is disposed without cancellation being requested.");
            }
        }
    }
}
