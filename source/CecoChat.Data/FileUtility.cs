namespace CecoChat.Data;

public interface IFileUtility
{
    /// <summary>
    /// The <see cref="extension"/> should have the dot '.' included, e.g.: ".png"
    /// </summary>
    bool IsExtensionKnown(string extension);

    /// <summary>
    /// The <see cref="extension"/> should have the dot '.' included, e.g.: ".png"
    /// </summary>
    string GetContentType(string extension);
}

public class FileUtility : IFileUtility
{
    private readonly IDictionary<string, string> _extMap;
    private readonly string _defaultContentType;

    public FileUtility()
    {
        _extMap = new Dictionary<string, string>();
        _defaultContentType = "application/octet-stream";

        _extMap.Add(".png", "image/png");
        _extMap.Add(".jpg", "image/jpg");
        _extMap.Add(".jpeg", "image/jpeg");
        _extMap.Add(".txt", "text/plain");
        _extMap.Add(".pdf", "application/pdf");
    }

    public bool IsExtensionKnown(string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(extension);

        return _extMap.ContainsKey(extension);
    }

    public string GetContentType(string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(extension);

        if (!_extMap.TryGetValue(extension, out string? contentType))
        {
            contentType = _defaultContentType;
        }

        return contentType;
    }
}
