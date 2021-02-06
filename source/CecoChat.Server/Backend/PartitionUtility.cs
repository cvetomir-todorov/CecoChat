using System;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Server.Backend
{
    public interface IPartitionUtility
    {
        int ChoosePartition(long userID, int partitionCount);
    }

    public sealed class PartitionUtility : IPartitionUtility
    {
        private readonly INonCryptoHash _hash;

        public PartitionUtility(
            INonCryptoHash hash)
        {
            _hash = hash;
        }

        public int ChoosePartition(long userID, int partitionCount)
        {
            int hash = _hash.Compute(userID);
            int partition = Math.Abs(hash) % partitionCount;
            return partition;
        }
    }

    public static class PartitionUtilityRegistrations
    {
        public static IServiceCollection AddPartitionUtility(this IServiceCollection services)
        {
            return services
                .AddSingleton<IPartitionUtility, PartitionUtility>()
                .AddSingleton<INonCryptoHash, FnvHash>();
        }
    }
}
