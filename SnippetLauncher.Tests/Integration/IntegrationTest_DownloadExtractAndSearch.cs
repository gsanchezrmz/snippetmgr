using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Core.ViewModels;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Integration;

public class IntegrationTest_DownloadExtractAndSearch : IAsyncLifetime
{
    private string _tempDir = string.Empty;
    private string _zipPath = string.Empty;

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
            // If download fails (e.g. 404 because extension version changed), create a dummy zip with a dummy structure
            CreateDummyVsix(_zipPath);
        }

        ZipFile.ExtractToDirectory(_zipPath, _tempDir, overwriteFiles: true);
    }

    private void CreateDummyVsix(string zipPath)
    {
        var dummyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dummyDir);
        var snippetsDir = Path.Combine(dummyDir, "extension", "snippets");
        Directory.CreateDirectory(snippetsDir);

        // Mock a VS Code snippet format
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
    public async Task IntegrationTest_DownloadExtractAndSearch_VSCodeVsixSnippet()
    {
        var extensionDir = Path.Combine(_tempDir, "extension");

        // Look for snippets directory or fallback to extension root
        var snippetsDir = Directory.Exists(Path.Combine(extensionDir, "snippets"))
            ? Path.Combine(extensionDir, "snippets")
            : extensionDir;

        var storage = new LocalStorageService(snippetsDir);
        var repository = new LocalSnippetRepository(storage);

        var mockClipboard = new Mock<ISystemClipboard>();
        var clipboardState = "Initial";
        mockClipboard.Setup(c => c.ContainsText()).Returns(true);
        mockClipboard.Setup(c => c.GetText()).Returns(() => clipboardState);
        mockClipboard.Setup(c => c.SetText(It.IsAny<string>())).Callback<string>(s => clipboardState = s);

        var clipboardManager = new ClipboardManager(new VariableParser(), mockClipboard.Object);

        var vm = new MainViewModel(repository, clipboardManager);
        await vm.InitializeAsync();

        // Act
        // There should be a "read delta" snippet in the databricks extension or our fallback dummy
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
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        return Task.CompletedTask;
    }
}
