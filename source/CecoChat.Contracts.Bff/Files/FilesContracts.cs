using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff.Files;

// TODO: encapsulate properties into a single File object
public sealed class UploadFileResponse
{
    [JsonPropertyName("bucket")]
    [AliasAs("bucket")]
    public string Bucket { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    [AliasAs("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class FileRef
{
    [JsonPropertyName("bucket")]
    [AliasAs("bucket")]
    public string Bucket { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    [AliasAs("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}
