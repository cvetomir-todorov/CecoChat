using System.Text.Json.Serialization;
using Refit;

namespace CecoChat.Contracts.Bff.Files;

public sealed class UploadFileResponse
{
    [JsonPropertyName("filePath")]
    [AliasAs("filePath")]
    public string FilePath { get; init; } = string.Empty;
}
