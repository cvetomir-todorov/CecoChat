using System.Buffers;
using Confluent.Kafka;
using ProtoBuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Messaging.Server.Backend.Production
{
    public sealed class KafkaProtobufSerializer<TMessage> : ISerializer<TMessage>
    {
        public byte[] Serialize(TMessage data, SerializationContext context)
        {
            ArrayBufferWriter<byte> array = new ArrayBufferWriter<byte>(initialCapacity: 128);
            Serializer.Serialize(array, data);
            return array.WrittenMemory.ToArray();
        }
    }
}
