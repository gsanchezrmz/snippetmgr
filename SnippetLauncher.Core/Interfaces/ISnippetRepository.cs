using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.Interfaces;

public interface ISnippetRepository
{
    Task<IEnumerable<Snippet>> GetAllSnippetsAsync();
    Task SaveSnippetAsync(Snippet snippet);
    Task DeleteSnippetAsync(string id);
    Task<IEnumerable<Snippet>> SearchSnippetsAsync(string query);
}
