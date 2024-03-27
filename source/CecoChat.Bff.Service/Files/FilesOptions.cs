namespace CecoChat.Bff.Service.Files;

public class FilesOptions
{
    public int MaxUploadedFileBytes { get; init; }
    public int MaxMultipartBodyBytes { get; init; }
    public int MaxRequestBodyBytes { get; init; }
}
