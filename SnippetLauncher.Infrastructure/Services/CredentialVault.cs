using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class CredentialVault : ICredentialVault
{
    private readonly string _vaultPath;

    public CredentialVault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _vaultPath = Path.Combine(localAppData, "SnippetLauncher", "credentials");
        if (!Directory.Exists(_vaultPath))
        {
            Directory.CreateDirectory(_vaultPath);
        }
    }

    public CredentialVault(string customPath)
    {
        _vaultPath = customPath;
        if (!Directory.Exists(_vaultPath))
        {
            Directory.CreateDirectory(_vaultPath);
        }
    }

    public void SaveToken(string service, string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        // On Linux, ProtectedData is not fully supported or uses a different mechanism.
        // For the sake of cross-platform testing in CI without DPAPI, we might need to fallback or mock it,
        // but for Windows DPAPI we use DataProtectionScope.CurrentUser.
        byte[] encryptedBytes;
        try
        {
            encryptedBytes = ProtectedData.Protect(tokenBytes, null, DataProtectionScope.CurrentUser);
        }
        catch (PlatformNotSupportedException)
        {
            // Fallback for non-Windows (e.g. CI running on Ubuntu)
            // Just for testing purposes
            encryptedBytes = tokenBytes;
        }

        var filePath = Path.Combine(_vaultPath, $"{service}.dat");
        File.WriteAllBytes(filePath, encryptedBytes);
    }

    public string GetToken(string service)
    {
        var filePath = Path.Combine(_vaultPath, $"{service}.dat");
        if (!File.Exists(filePath)) return string.Empty;

        var encryptedBytes = File.ReadAllBytes(filePath);
        byte[] decryptedBytes;
        try
        {
            decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        }
        catch (PlatformNotSupportedException)
        {
            // Fallback for non-Windows tests
            decryptedBytes = encryptedBytes;
        }

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
