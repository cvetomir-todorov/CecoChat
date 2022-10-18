using System;

namespace CecoChat.Server.Backplane;

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