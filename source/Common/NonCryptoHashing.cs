namespace Common;

public interface INonCryptoHash
{
    int Compute(long value);
}

/// <summary>
/// A stable hashing using FNV algorithm.
/// </summary>
public sealed class FnvHash : INonCryptoHash
{
    public int Compute(long value)
    {
        short value0 = (short)(value >> 48);
        short value1 = (short)(value >> 32);
        short value2 = (short)(value >> 16);
        short value3 = (short)value;

        int hash = 92821;
        const int prime = 486187739;

        unchecked // overflow is fine
        {
            hash = (hash * prime) ^ value0;
            hash = (hash * prime) ^ value1;
            hash = (hash * prime) ^ value2;
            hash = (hash * prime) ^ value3;
        }

        return hash;
    }
}

/// <summary>
/// A non-stable .NET hashing function using xxHash algorithm.
/// </summary>
public sealed class XxHash : INonCryptoHash
{
    public int Compute(long value)
    {
        short value1 = (short)(value >> 48);
        short value2 = (short)(value >> 32);
        short value3 = (short)(value >> 16);
        short value4 = (short)value;

        int hash = HashCode.Combine(value1, value2, value3, value4);
        return hash;
    }
}
