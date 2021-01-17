using System;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Servers
{
    public interface ITopicPartitionFlyweight
    {
        TopicPartition GetMessagesTopicPartition(int partition);
    }

    public sealed class TopicPartitionFlyweight : ITopicPartitionFlyweight
    {
        private readonly IKafkaOptions _options;
        private readonly TopicPartition[] _messageTopicPartitions;

        public TopicPartitionFlyweight(IOptions<KafkaOptions> options)
        {
            _options = options.Value;
            _messageTopicPartitions = new TopicPartition[_options.MessagesTopicPartitionCount];

            for (int partition = 0; partition < _options.MessagesTopicPartitionCount; ++partition)
            {
                _messageTopicPartitions[partition] = new TopicPartition(_options.MessagesTopic, partition);
            }
        }

        public TopicPartition GetMessagesTopicPartition(int partition)
        {
            if (partition < 0 || partition >= _messageTopicPartitions.Length)
                throw new ArgumentException($"{nameof(partition)} should be within [0, {_messageTopicPartitions.Length - 1}].");

            return _messageTopicPartitions[partition];
        }
    }
}
