using System;
using System.Buffers;
using ProtoBuf;

namespace CecoChat.ProtobufNet
{
    public sealed class ProtobufSerializer
    {
        public byte[] SerializeToByteArray<TMessage>(TMessage message)
        {
            ArrayBufferWriter<byte> array = new ArrayBufferWriter<byte>(initialCapacity: 128);
            Serializer.Serialize(array, message);
            return array.WrittenMemory.ToArray();
        }

        public TMessage DeserializeFromSpan<TMessage>(ReadOnlySpan<byte> data)
        {
            return Serializer.Deserialize<TMessage>(data);
        }
    }
}
