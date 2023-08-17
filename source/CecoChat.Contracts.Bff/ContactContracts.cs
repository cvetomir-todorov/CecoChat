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
    Pending,
    Connected
}

public sealed class GetContactsResponse
{
    [JsonPropertyName("contacts")]
    [AliasAs("contacts")]
    public Contact[] Contacts { get; init; } = Array.Empty<Contact>();
}

public sealed class InviteContactRequest
{ }

public sealed class InviteContactResponse
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class ApproveContactRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class ApproveContactResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public Guid NewVersion { get; set; }
}

public sealed class CancelContactRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class CancelContactResponse
{ }

public sealed class RemoveContactRequest
{
    [JsonPropertyName("version")]
    [AliasAs("version")]
    public Guid Version { get; init; }
}

public sealed class RemoveContactResponse
{ }
