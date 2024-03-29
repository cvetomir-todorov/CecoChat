using Common;

namespace CecoChat.Backplane;

public interface IPartitioner
{
    int ChoosePartition(long userId, int partitionCount);
}

public sealed class Partitioner : IPartitioner
{
    private readonly INonCryptoHash _hash;

    public Partitioner(
        INonCryptoHash hash)
    {
        _hash = hash;
    }

    public int ChoosePartition(long userId, int partitionCount)
    {
        int hash = _hash.Compute(userId);
        int partition = Math.Abs(hash) % partitionCount;
        return partition;
    }
}
