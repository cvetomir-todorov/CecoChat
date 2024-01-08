using System.Collections.Concurrent;

namespace CecoChat.ConsoleClient.LocalStorage;

public class FileStorage
{
    private readonly ConcurrentDictionary<string, FileRef> _userFiles;

    public FileStorage()
    {
        _userFiles = new();
    }

    public IEnumerable<FileRef> EnumerateUserFiles()
    {
        foreach (KeyValuePair<string, FileRef> pair in _userFiles)
        {
            yield return pair.Value;
        }
    }

    public void UpdateUserFiles(IEnumerable<FileRef> files)
    {
        foreach (FileRef file in files)
        {
            UpdateUserFile(file);
        }
    }

    public void UpdateUserFile(FileRef file)
    {
        _userFiles.AddOrUpdate(
            key: $"{file.Bucket}/{file.Path}",
            addValueFactory: _ => file,
            updateValueFactory: (_, existing) =>
            {
                existing.Version = file.Version;
                return existing;
            });
    }
}
