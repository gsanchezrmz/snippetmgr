namespace SnippetLauncher.Core.Interfaces;

public interface ICredentialVault
{
    void SaveToken(string service, string token);
    string GetToken(string service);
}
