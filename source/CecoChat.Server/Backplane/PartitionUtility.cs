namespace CecoChat.Server.Backplane;

public interface IPartitionUtility
{
    int ChoosePartition(long userId, int partitionCount);
}

public sealed class PartitionUtility : IPartitionUtility
{
    private readonly INonCryptoHash _hash;

    public PartitionUtility(
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
