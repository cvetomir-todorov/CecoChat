namespace CecoChat.Server.Bff.Files;

public class FilesOptions
{
    public int MaxUploadedFileBytes { get; init; }
    public int MaxMultipartBodyBytes { get; init; }
    public int MaxRequestBodyBytes { get; init; }
}
