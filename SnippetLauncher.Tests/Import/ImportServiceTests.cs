using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Import;

public class ImportServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly SqliteConnection _keepAliveConnection;

    public ImportServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnippetLauncherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        _keepAliveConnection = new SqliteConnection("Data Source=SnippetLauncherTest;Mode=Memory;Cache=Shared");
        _keepAliveConnection.Open();
    }

    [Fact]
    public async Task UnitTest_ImportLocalDirectory_ParsesAndInsertsCorrectly()
    {
        // Arrange
        var testResourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResources", "snippet");

        var repo = new SQLiteSnippetRepository("Data Source=SnippetLauncherTest;Mode=Memory;Cache=Shared");
        var importService = new SmartImportService(repo);

        // Act
        var result = await importService.ImportFromDirectoryAsync(testResourcesDir);

        // Assert
        result.Found.Should().BeGreaterThan(0);
        result.New.Should().BeGreaterThan(0);

        var snippets = await repo.GetAllSnippetsAsync();
        snippets.Should().Contain(s =>
            s.Title == "Saber qué cambios se han hecho en los procedimientos almacenados en los últimos X días" &&
            s.Language == "sql" &&
            s.Code.Contains("SELECT name") &&
            s.Code.Contains("AND DATEDIFF(D,create_date, GETDATE()) < 7")
        );
    }

    public void Dispose()
    {
        _keepAliveConnection.Close();
        _keepAliveConnection.Dispose();

        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }
}
