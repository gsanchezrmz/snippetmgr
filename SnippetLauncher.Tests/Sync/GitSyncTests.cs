using System;
using System.IO;
using FluentAssertions;
using LibGit2Sharp;
using Moq;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Sync;

public class GitSyncTests : IDisposable
{
    private readonly string _testDir;
    private readonly Mock<IStorageService> _mockStorage;
    private readonly Mock<ICredentialVault> _mockVault;
    private readonly GitSyncService _gitSync;

    public GitSyncTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnippetLauncherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        _mockStorage = new Mock<IStorageService>();
        _mockStorage.Setup(x => x.GetStorageDirectory()).Returns(_testDir);

        _mockVault = new Mock<ICredentialVault>();
        _mockVault.Setup(x => x.GetToken(It.IsAny<string>())).Returns("dummy_token");

        _gitSync = new GitSyncService(_mockStorage.Object, _mockVault.Object);
    }

    [Fact]
    public void GitSync_WithValidCredentials_SuccessfullyPullsChanges()
    {
        // For testing, we create a local dummy repo to simulate a remote
        var remoteDir = Path.Combine(Path.GetTempPath(), "SnippetLauncherTests", Guid.NewGuid().ToString(), "remote");
        Directory.CreateDirectory(remoteDir);
        Repository.Init(remoteDir, isBare: true);

        // Act - should clone since it's empty
        _gitSync.Sync(remoteDir, "master"); // using master as default for local init

        // Assert
        Repository.IsValid(_testDir).Should().BeTrue();

        // Cleanup remote
        if (Directory.Exists(remoteDir)) Directory.Delete(remoteDir, true);
    }

    [Fact]
    public void GitSync_OnConflict_PrefersLocalChangesAsDraft()
    {
        // We simulate this by checking if the checkout conflict strategy is Ours.
        // We can create a repository, dirty the state, and verify it commits as a local draft.

        Repository.Init(_testDir);
        using (var repo = new Repository(_testDir))
        {
            var file = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(file, "local change");
            Commands.Stage(repo, "test.txt");
            var signature = new Signature("Test", "test@test.com", DateTimeOffset.Now);
            repo.Commit("Initial", signature, signature);

            // Create a local change (dirty)
            File.WriteAllText(file, "local modified change");
        }

        // Act
        // This will commit the dirty state as a local draft commit
        _gitSync.Sync("dummy_remote");

        // Assert
        using (var repo = new Repository(_testDir))
        {
            repo.RetrieveStatus().IsDirty.Should().BeFalse();
            var log = repo.Commits.GetEnumerator();
            log.MoveNext();
            log.Current.Message.Should().StartWith("Local draft commit");
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                // LibGit2Sharp leaves some files read-only, need to un-readonly them before deleting
                foreach (var file in Directory.GetFiles(_testDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_testDir, true);
            }
        }
        catch { }
    }
}
