using System.Threading.Tasks;

namespace SnippetLauncher.Core.Interfaces;

public interface IGitSyncService
{
    void Sync(string remoteUrl, string branch = "main");
}
