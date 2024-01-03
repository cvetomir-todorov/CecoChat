using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff.Files;

public sealed class UploadFileResponse
{
    [JsonPropertyName("file")]
    [AliasAs("file")]
    public FileRef File { get; set; } = new();
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

public sealed class DownloadFileRequest
{
    [JsonPropertyName("bucket")]
    [AliasAs("bucket")]
    public string Bucket { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    [AliasAs("path")]
    public string Path { get; init; } = string.Empty;
}
