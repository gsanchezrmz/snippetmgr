using System;
using System.IO;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _storageDirectory;

    public LocalStorageService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(localAppData, "SnippetLauncher", "snippets");
    }

    public string GetStorageDirectory() => _storageDirectory;

    public void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }
}
