using System;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface ITopicPartitionFlyweight
    {
        TopicPartition GetMessagesTopicPartition(int partition);
    }

    public sealed class TopicPartitionFlyweight : ITopicPartitionFlyweight
    {
        private readonly TopicPartition[] _messageTopicPartitions;

        public TopicPartitionFlyweight(IOptions<BackendOptions> options)
        {
            _messageTopicPartitions = new TopicPartition[options.Value.MessagesTopicPartitionCount];

            for (int partition = 0; partition < options.Value.MessagesTopicPartitionCount; ++partition)
            {
                _messageTopicPartitions[partition] = new TopicPartition(options.Value.MessagesTopicName, partition);
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
