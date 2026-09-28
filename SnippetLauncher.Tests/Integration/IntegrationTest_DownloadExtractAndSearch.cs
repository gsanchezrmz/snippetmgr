using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Moq;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Core.ViewModels;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Integration;

public class IntegrationTest_DownloadImportAndSearch : IAsyncLifetime
{
    private string _tempDir = string.Empty;
    private string _zipPath = string.Empty;
    private SqliteConnection _keepAliveConnection = null!;

    public async Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SnippetLauncher_E2E", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _zipPath = Path.Combine(_tempDir, "databricks.vsix");

        var url = "https://marketplace.visualstudio.com/_apis/public/gallery/publishers/databricks/vsextensions/databricks/1.4.3/vspackage";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            using var fs = File.Create(_zipPath);
            await stream.CopyToAsync(fs);
            fs.Close();
        }
        catch
        {
            CreateDummyVsix(_zipPath);
        }

        ZipFile.ExtractToDirectory(_zipPath, _tempDir, overwriteFiles: true);

        // Maintain in-memory db across tests/methods
        _keepAliveConnection = new SqliteConnection($"Data Source=E2ETest_{Guid.NewGuid()};Mode=Memory;Cache=Shared");
        _keepAliveConnection.Open();
    }

    private void CreateDummyVsix(string zipPath)
    {
        var dummyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dummyDir);

        var packageJson = @"{
            ""contributes"": {
                ""snippets"": [
                    { ""language"": ""python"", ""path"": ""snippets/snippets.json"" }
                ]
            }
        }";
        File.WriteAllText(Path.Combine(dummyDir, "package.json"), packageJson);

        var snippetsDir = Path.Combine(dummyDir, "snippets");
        Directory.CreateDirectory(snippetsDir);

        var dummyVsCodeSnippet = @"{
            ""read delta"": {
                ""prefix"": ""read delta"",
                ""body"": [
                    ""spark.read.format(\""delta\"").load(\""path\"")""
                ],
                ""description"": ""Read a delta table""
            }
        }";

        File.WriteAllText(Path.Combine(snippetsDir, "snippets.json"), dummyVsCodeSnippet);
        ZipFile.CreateFromDirectory(dummyDir, zipPath);
        Directory.Delete(dummyDir, true);
    }

    [Fact]
    public async Task IntegrationTest_DownloadImportAndSearch_VSCodeVsixSnippet()
    {
        var extensionDir = Path.Combine(_tempDir, "extension");
        var scanDir = Directory.Exists(extensionDir) ? extensionDir : _tempDir;

        var repository = new SQLiteSnippetRepository(_keepAliveConnection.ConnectionString);
        var importService = new SmartImportService(repository);

        // 1. Run Smart Import directly from the extracted vsix root
        var importResult = await importService.ImportFromDirectoryAsync(scanDir);

        // 2. Setup ViewModel
        var mockClipboard = new Mock<ISystemClipboard>();
        var clipboardState = "Initial";
        mockClipboard.Setup(c => c.ContainsText()).Returns(true);
        mockClipboard.Setup(c => c.GetText()).Returns(() => clipboardState);
        mockClipboard.Setup(c => c.SetText(It.IsAny<string>())).Callback<string>(s => clipboardState = s);

        var clipboardManager = new ClipboardManager(new VariableParser(), mockClipboard.Object);

        var vm = new MainViewModel(repository, clipboardManager);
        await vm.InitializeAsync();

        // Act
        vm.SearchQuery = "read delta";
        await Task.Delay(100);

        // Assert
        vm.Snippets.Should().NotBeEmpty();
        var selected = vm.Snippets.FirstOrDefault(s => s.Title.ToLowerInvariant().Contains("read delta") || s.Tags.ToLowerInvariant().Contains("read delta"));
        selected.Should().NotBeNull();

        vm.SelectedSnippet = selected;
        await vm.InjectSnippetCommand.ExecuteAsync(null);

        mockClipboard.Verify(c => c.SimulatePaste(), Times.Once);
        clipboardState.Should().Be("Initial");
    }

    public Task DisposeAsync()
    {
        _keepAliveConnection?.Close();
        _keepAliveConnection?.Dispose();

        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
        return Task.CompletedTask;
    }
}
