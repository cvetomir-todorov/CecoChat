using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Bff.Contracts.Files;

public sealed class GetUserFilesRequest
{
    [JsonPropertyName("newerThan")]
    [AliasAs("newerThan")]
    public DateTime NewerThan { get; init; }
}

public sealed class GetUserFilesResponse
{
    [JsonPropertyName("files")]
    [AliasAs("files")]
    public FileRef[] Files { get; init; } = Array.Empty<FileRef>();
}

public sealed class UploadFileResponse
{
    [JsonPropertyName("file")]
    [AliasAs("file")]
    public FileRef File { get; set; } = new();
}

public sealed class AddFileAccessRequest
{
    [JsonPropertyName("allowedUserId")]
    [AliasAs("allowedUserId")]
    public long AllowedUserId { get; init; }

    [JsonPropertyName("version")]
    [AliasAs("version")]
    public DateTime Version { get; init; }
}

public sealed class AddFileAccessResponse
{
    [JsonPropertyName("newVersion")]
    [AliasAs("newVersion")]
    public DateTime NewVersion { get; init; }
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
