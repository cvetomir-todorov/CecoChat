namespace Common.Kafka;

public readonly struct PartitionRange : IEquatable<PartitionRange>
{
    public PartitionRange(ushort lower, ushort upper)
    {
        if (lower > upper)
            throw new ArgumentException($"'{nameof(lower)}' should be <= '{nameof(upper)}'.");

        Lower = lower;
        Upper = upper;
        IsNotEmpty = true;
    }

    private PartitionRange(ushort lower, ushort upper, bool isNotEmpty)
    {
        Lower = lower;
        Upper = upper;
        IsNotEmpty = isNotEmpty;
    }

    public ushort Lower { get; }
    public ushort Upper { get; }
    public bool IsNotEmpty { get; }

    public ushort Length
    {
        get
        {
            checked
            {
                return (ushort)(Upper - Lower + 1);
            }
        }
    }

    public bool Contains(int partition)
    {
        bool contains = IsNotEmpty && partition >= Lower && partition <= Upper;
        return contains;
    }

    public override string ToString()
    {
        return IsNotEmpty ? $"[{Lower}, {Upper}]" : "[,]";
    }

    public bool Equals(PartitionRange other)
    {
        return Lower == other.Lower &&
               Upper == other.Upper &&
               IsNotEmpty == other.IsNotEmpty;
    }

    public override bool Equals(object? obj)
    {
        return obj is PartitionRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = HashCode.Combine(Lower, Upper);
        return hashCode;
    }

    public static bool operator ==(PartitionRange left, PartitionRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PartitionRange left, PartitionRange right)
    {
        return !left.Equals(right);
    }

    public static readonly PartitionRange Empty = new(0, 0, isNotEmpty: false);

    public static bool TryParse(string value, char separator, out PartitionRange partitionRange)
    {
        int separatorIndex = value.IndexOf(separator);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            partitionRange = Empty;
            return false;
        }

        string lowerString = value.Substring(startIndex: 0, length: separatorIndex);
        string upperString = value.Substring(startIndex: separatorIndex + 1);

        if (!ushort.TryParse(lowerString, out ushort lower) ||
            !ushort.TryParse(upperString, out ushort upper))
        {
            partitionRange = Empty;
            return false;
        }

        partitionRange = new PartitionRange(lower, upper);
        return true;
    }
}
