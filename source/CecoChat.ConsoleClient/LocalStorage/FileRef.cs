namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class FileRef
{
    public string Bucket { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public DateTime Version { get; set; }

    public override string ToString()
    {
        return $"{Bucket}/{Path} {Version}";
    }
}
