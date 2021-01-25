using System;
using CecoChat.Contracts.Backend;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IPartitionUtility
    {
        int ChoosePartition(Message message);
    }

    public sealed class PartitionUtility : IPartitionUtility
    {
        private readonly IBackendOptions _options;
        private readonly INonCryptoHash _hash;

        public PartitionUtility(
            IOptions<BackendOptions> options,
            INonCryptoHash hash)
        {
            _options = options.Value;
            _hash = hash;
        }

        public int ChoosePartition(Message message)
        {
            int hash = _hash.Compute(message.ReceiverID);
            int partition = Math.Abs(hash) % _options.MessagesTopicPartitionCount;
            return partition;
        }
    }
}
