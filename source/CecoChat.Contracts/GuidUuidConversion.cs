using System.Buffers.Binary;

namespace CecoChat.Contracts;

public static class GuidUuidConversion
{
    public static Uuid ToUuid(this Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!guid.TryWriteBytes(bytes))
        {
            throw new InvalidOperationException($"Failed to write {guid} to bytes with length {bytes.Length}.");
        }

        // MSB -> LSB: time_low (32 bits) | time_mid (16 bits) | time_hi_and_version (16 bits)
        ulong timeLow = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(0, 4)) << 32;
        ulong timeMid = (ulong)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(4, 2)) << 16;
        ulong timeHiAndVersion = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(6, 2));

        Uuid uuid = new();
        uuid.High64 = timeLow | timeMid | timeHiAndVersion;
        // MSB -> LSB: clock_seq_hi_and_reserved (8 bits) | clock_seq_low (8 bits) | node (48 bits)
        uuid.Low64 = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));

        return uuid;
    }

    public static Guid ToGuid(this Uuid uuid)
    {
        Span<byte> bytes = stackalloc byte[16];

        // MSB -> LSB: time_low (32 bits) | time_mid (16 bits) | time_hi_and_version (16 bits)
        uint timeLow = (uint)(uuid.High64 >> 32);
        ushort timeMid = (ushort)((uuid.High64 >> 16) & 0xFFFF);
        ushort timeHiAndVersion = (ushort)(uuid.High64 & 0xFFFF);

        BinaryPrimitives.WriteUInt32LittleEndian(bytes.Slice(0, 4), timeLow);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(4, 2), timeMid);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.Slice(6, 2), timeHiAndVersion);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(8, 8), uuid.Low64);

        return new Guid(bytes);
    }
}
