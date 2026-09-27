namespace SnippetLauncher.Core.Interfaces;

public interface IStorageService
{
    string GetStorageDirectory();
    void EnsureStorageDirectoryExists();
}
