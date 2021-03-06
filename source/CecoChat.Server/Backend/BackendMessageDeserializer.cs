﻿using System;
using CecoChat.Contracts.Backend;
using Confluent.Kafka;
using Google.Protobuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace CecoChat.Server.Backend
{
    public sealed class BackendMessageDeserializer : IDeserializer<BackendMessage>
    {
        public BackendMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
                return default;

            BackendMessage message = new BackendMessage();
            message.MergeFrom(data.ToArray());
            return message;
        }
    }
}
