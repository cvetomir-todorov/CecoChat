using System;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IPartitionUtility
    {
        int ChoosePartition(long receiverID);
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

        public int ChoosePartition(long receiverID)
        {
            int hash = _hash.Compute(receiverID);
            int partition = Math.Abs(hash) % _options.MessagesTopicPartitionCount;
            return partition;
        }
    }
}
