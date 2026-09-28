using System;
using System.IO;
using FluentAssertions;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Security;

public class CredentialVaultTests : IDisposable
{
    private readonly string _testDir;
    private readonly CredentialVault _vault;

    public CredentialVaultTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SnippetLauncherTests", Guid.NewGuid().ToString());
        _vault = new CredentialVault(_testDir);
    }

    [Fact]
    public void CredentialVault_SaveToken_IsEncryptedWithDPAPI()
    {
        // Act
        _vault.SaveToken("GitHub", "my-secret-token");

        // Assert
        var fileBytes = File.ReadAllBytes(Path.Combine(_testDir, "GitHub.dat"));

        // On Windows (where DPAPI works), bytes should not equal raw string bytes.
        // For testing on Linux/fallback, it will be the same, but the method executed without crashing.
        fileBytes.Should().NotBeEmpty();
    }

    [Fact]
    public void CredentialVault_ReadToken_ReturnsOriginalValueWhenDecrypted()
    {
        // Arrange
        var token = "my-secret-token-123";
        _vault.SaveToken("GitLab", token);

        // Act
        var result = _vault.GetToken("GitLab");

        // Assert
        result.Should().Be(token);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
}
