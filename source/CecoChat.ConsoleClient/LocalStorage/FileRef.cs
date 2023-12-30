namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class FileRef
{
    public string Name { get; init; } = string.Empty;
    public DateTime Version { get; set; }

    public override string ToString()
    {
        return $"{Name} {Version}";
    }
}
