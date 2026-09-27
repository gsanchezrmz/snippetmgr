using System.Threading.Tasks;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.Interfaces;

public interface IMarketService
{
    Task<Snippet> DownloadSnippetAsync(string url);
}
