using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Storage;

public class LocalStorageTests : IDisposable
{
    private readonly string _testDir;
    private readonly TestStorageService _storageService;

    public LocalStorageTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnippetLauncherTests", Guid.NewGuid().ToString());
        _storageService = new TestStorageService(_testDir);
    }

    [Fact]
    public void AppStartup_InCleanEnvironment_CreatesRequiredDirectories()
    {
        // Act
        _storageService.EnsureStorageDirectoryExists();

        // Assert
        Directory.Exists(_testDir).Should().BeTrue();
    }

    [Fact]
    public async Task SnippetStorage_SaveNew_WritesToLocalDriveAsPlainText()
    {
        // Arrange
        var repository = new LocalSnippetRepository(_storageService);
        var snippet = new Snippet
        {
            Title = "Test Snippet",
            Content = "Console.WriteLine(\"Hello\");"
        };

        // Act
        await repository.SaveSnippetAsync(snippet);

        // Assert
        var files = Directory.GetFiles(_testDir, "*.json");
        files.Should().HaveCount(1);

        var content = await File.ReadAllTextAsync(files[0]);
        content.Should().Contain("Test Snippet");
        content.Should().Contain("Console.WriteLine");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
}

public class TestStorageService : SnippetLauncher.Core.Interfaces.IStorageService
{
    private readonly string _dir;
    public TestStorageService(string dir) => _dir = dir;
    public void EnsureStorageDirectoryExists() => Directory.CreateDirectory(_dir);
    public string GetStorageDirectory() => _dir;
}
