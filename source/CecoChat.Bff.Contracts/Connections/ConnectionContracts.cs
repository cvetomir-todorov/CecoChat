using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Connections;

public sealed class Connection
{
    [JsonPropertyName("connectionId")]
    [AliasAs("connectionId")]
    public long ConnectionId { get; init; }

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }

    [JsonPropertyName("status")]
    [AliasAs("status")]
    public ConnectionStatus Status { get; init; }
}

public enum ConnectionStatus
{
    NotConnected,
    Pending,
    Connected
}

public sealed class GetConnectionsResponse
{
    [JsonPropertyName("connections")]
    [AliasAs("connections")]
    public Connection[] Connections { get; init; } = Array.Empty<Connection>();
}

public sealed class InviteConnectionRequest
{ }

public sealed class InviteConnectionResponse
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class ApproveConnectionRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class ApproveConnectionResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; set; }
}

public sealed class CancelConnectionRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class CancelConnectionResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; set; }
}

public sealed class RemoveConnectionRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class RemoveConnectionResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; set; }
}
