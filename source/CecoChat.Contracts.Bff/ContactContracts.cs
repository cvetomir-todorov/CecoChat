using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff;

public sealed class Contact
{
    [JsonPropertyName("contactUserId")]
    [AliasAs("contactUserId")]
    public long ContactUserId { get; init; }

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }

    [JsonPropertyName("status")]
    [AliasAs("status")]
    public string Status { get; init; } = string.Empty;
}

public enum ContactStatus
{
    PendingRequest,
    CancelledRequest,
    Established,
    Removed
}

public sealed class GetContactsResponse
{
    [JsonPropertyName("contacts")]
    [AliasAs("contacts")]
    public Contact[] Contacts { get; init; } = Array.Empty<Contact>();
}
