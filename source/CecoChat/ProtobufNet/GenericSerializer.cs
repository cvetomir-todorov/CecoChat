using System;
using System.Buffers;
using ProtoBuf;

namespace CecoChat.ProtobufNet
{
    // TODO: rename to protobuf serializer
    public sealed class GenericSerializer<TMessage>
    {
        public byte[] SerializeToByteArray(TMessage message)
        {
            ArrayBufferWriter<byte> array = new ArrayBufferWriter<byte>(initialCapacity: 128);
            Serializer.Serialize(array, message);
            return array.WrittenMemory.ToArray();
        }

        public TMessage DeserializeFromSpan(ReadOnlySpan<byte> data)
        {
            return Serializer.Deserialize<TMessage>(data);
        }
    }
}
