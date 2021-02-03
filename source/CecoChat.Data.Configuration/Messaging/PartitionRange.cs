using System;

namespace CecoChat.Data.Configuration.Messaging
{
    public readonly struct PartitionRange
    {
        public PartitionRange(ushort lower, ushort upper)
        {
            if (lower > upper)
                throw new ArgumentException($"'{nameof(lower)}' should be <= '{nameof(upper)}'.");

            Lower = lower;
            Upper = upper;
        }

        public ushort Lower { get; }
        public ushort Upper { get; }

        public ushort Length
        {
            get
            {
                checked
                {
                    return (ushort) (Upper - Lower + 1);
                }
            }
        }

        public override string ToString() => $"[{Lower}, {Upper}]";

        public static readonly PartitionRange Zero = new(0, 0);

        public static bool TryParse(string value, char separator, out PartitionRange partitionRange)
        {
            int separatorIndex = value.IndexOf(separator);
            if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
            {
                partitionRange = Zero;
                return false;
            }

            string lowerString = value.Substring(startIndex: 0, length: separatorIndex);
            string upperString = value.Substring(startIndex: separatorIndex + 1);

            if (!ushort.TryParse(lowerString, out ushort lower) ||
                !ushort.TryParse(upperString, out ushort upper))
            {
                partitionRange = Zero;
                return false;
            }

            partitionRange = new PartitionRange(lower, upper);
            return true;
        }
    }
}