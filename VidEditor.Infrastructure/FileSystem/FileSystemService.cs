namespace VidEditor.Infrastructure.FileSystem;

public sealed class FileSystemService : IFileSystemService
{
    public bool Exists(string path) => File.Exists(path);

    public long GetLength(string path) => new FileInfo(path).Length;
}
