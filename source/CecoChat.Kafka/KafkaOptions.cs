﻿namespace CecoChat.Kafka;

public sealed class KafkaOptions
{
    public string[] BootstrapServers { get; init; } = Array.Empty<string>();
}
