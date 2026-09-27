using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Tests.Storage;
using Xunit;

namespace SnippetLauncher.Tests.Search;

public class FuzzySearchTests
{
    [Fact]
    public async Task Search_FuzzyMatch_ReturnsItemsInRelevanceOrder()
    {
        // Arrange
        var testDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnippetLauncherTests", System.Guid.NewGuid().ToString());
        var storage = new TestStorageService(testDir);
        var repo = new LocalSnippetRepository(storage);

        await repo.SaveSnippetAsync(new Snippet { Title = "Entity Framework Core Setup", Description = "EF Core configuration" });
        await repo.SaveSnippetAsync(new Snippet { Title = "ASP.NET Core Middleware", Description = "Custom middleware" });
        await repo.SaveSnippetAsync(new Snippet { Title = "React Hooks Component", Description = "useEffect and useState" });

        // Act
        var results = (await repo.SearchSnippetsAsync("core setup")).ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.First().Title.Should().Be("Entity Framework Core Setup");

        // Cleanup
        System.IO.Directory.Delete(testDir, true);
    }
}
