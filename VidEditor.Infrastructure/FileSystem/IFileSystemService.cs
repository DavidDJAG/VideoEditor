namespace VidEditor.Infrastructure.FileSystem;

public interface IFileSystemService
{
    bool Exists(string path);

    long GetLength(string path);
}
