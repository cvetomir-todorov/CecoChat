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

        public PartitionUtility(IOptions<BackendOptions> options)
        {
            _options = options.Value;
        }

        public int ChoosePartition(Message message)
        {
            int hash = FnvHash(message.ReceiverID);
            int partition = Math.Abs(hash) % _options.MessagesTopicPartitionCount;
            return partition;
        }

        private static int FnvHash(int value)
        {
            byte byte0 = (byte) (value >> 24);
            byte byte1 = (byte) (value >> 16);
            byte byte2 = (byte) (value >> 8);
            byte byte3 = (byte) value;

            int hash = 92821;
            const int prime = 486187739;

            unchecked // overflow is fine
            {
                hash = hash * prime + byte0;
                hash = hash * prime + byte1;
                hash = hash * prime + byte2;
                hash = hash * prime + byte3;
            }

            return hash;
        }
    }
}
