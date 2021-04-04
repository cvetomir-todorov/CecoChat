using System;
using System.Diagnostics;
using System.Threading;
using CecoChat.Kafka.Instrumentation;
using CecoChat.Tracing;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Kafka
{
    public interface IKafkaProducer<TKey, TValue> : IDisposable
    {
        void Initialize(IKafkaOptions options, IKafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer);

        void Produce(Message<TKey, TValue> message, TopicPartition topicPartition);

        void Produce(Message<TKey, TValue> message, string topic);

        void FlushPendingMessages();
    }

    public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
    {
        private readonly ILogger _logger;
        private readonly IActivityUtility _activityUtility;
        private readonly IKafkaActivityUtility _kafkaActivityUtility;
        private IProducer<TKey, TValue> _producer;
        private IKafkaProducerOptions _producerOptions;
        private string _id;

        public KafkaProducer(
            ILogger<KafkaProducer<TKey, TValue>> logger,
            IActivityUtility activityUtility,
            IKafkaActivityUtility kafkaActivityUtility)
        {
            _logger = logger;
            _activityUtility = activityUtility;
            _kafkaActivityUtility = kafkaActivityUtility;
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }

        public void Initialize(IKafkaOptions options, IKafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"'{nameof(Initialize)}' already called.");
            }

            ProducerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', options.BootstrapServers),
                Acks = producerOptions.Acks,
                LingerMs = producerOptions.LingerMs,
                MessageTimeoutMs = producerOptions.MessageTimeoutMs,
                MessageSendMaxRetries = producerOptions.MessageSendMaxRetries
            };

            _producer = new ProducerBuilder<TKey, TValue>(configuration)
                .SetValueSerializer(valueSerializer)
                .Build();
            _producerOptions = producerOptions;
            _id = $"{KafkaProducerIDGenerator.GetNextID()}@{producerOptions.IDContext}";
        }

        public void Produce(Message<TKey, TValue> message, TopicPartition topicPartition)
        {
            Activity activity = StartActivity(message, topicPartition.Topic, topicPartition.Partition);
            _producer.Produce(topicPartition, message, deliveryReport => DeliveryHandler(deliveryReport, activity));
            _logger.LogTrace("Producer {0} produced message {1} in {2}{3}.", _id, message.Value, topicPartition.Topic, topicPartition.Partition);
        }

        public void Produce(Message<TKey, TValue> message, string topic)
        {
            Activity activity = StartActivity(message, topic);
            _producer.Produce(topic, message, deliveryReport => DeliveryHandler(deliveryReport, activity));
            _logger.LogTrace("Producer {0} produced message {1} in {2}.", _id, message.Value, topic);
        }

        public void FlushPendingMessages()
        {
            if (_producer == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Producer {0} flushing pending messages...", _id);
                _producer.Flush();
                _logger.LogInformation("Producer {0} flushing pending messages succeeded.", _id);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Producer {0} flushing pending messages failed.", _id);
            }
        }

        private Activity StartActivity(Message<TKey, TValue> message, string topic, int? partition = null)
        {
            Activity activity = KafkaInstrumentation.ActivitySource.StartActivity(KafkaInstrumentation.Operations.Production, ActivityKind.Producer);
            if (activity == null)
            {
                return null;
            }

            // activity will be completed in the delivery handler thread and we don't want to pollute the execution context
            // so we set the current activity to the previous one
            Activity.Current = activity.Parent;

            string displayName = $"{activity.OperationName}/Producer:{_producerOptions.IDContext} -> Topic:{topic}";
            _kafkaActivityUtility.EnrichActivity(topic, partition, displayName, activity);
            _kafkaActivityUtility.InjectTraceData(activity, message);

            return activity;
        }

        private void DeliveryHandler(DeliveryReport<TKey, TValue> report, Activity activity)
        {
            bool success = true;
            try
            {
                TValue value = report.Message.Value;

                if (report.Status != PersistenceStatus.Persisted)
                {
                    _logger.LogError("Message {0} persistence status {1}.", value, report.Status);
                    success = false;
                }
                if (report.Error.IsError)
                {
                    _logger.LogError("Message {0} error '{1}'.", value, report.Error.Reason);
                    success = false;
                }
                if (report.TopicPartitionOffsetError.Error.IsError)
                {
                    _logger.LogError("Message {0} topic partition {1} error '{2}'.",
                        value, report.TopicPartitionOffsetError.Partition, report.TopicPartitionOffsetError.Error.Reason);
                    success = false;
                }
            }
            finally
            {
                // do not change the Activity.Current
                _activityUtility.Stop(activity, success, relyOnDefaultPolicyOfSettingCurrentActivity: false);
            }
        }
    }

    /// <summary>
    /// Not inside the <see cref="KafkaProducer{TKey,TValue}"/> class which uses it since it is generic.
    /// </summary>
    internal static class KafkaProducerIDGenerator
    {
        private static int _nextIDCounter;

        public static int GetNextID()
        {
            return Interlocked.Increment(ref _nextIDCounter);
        }
    }
}
