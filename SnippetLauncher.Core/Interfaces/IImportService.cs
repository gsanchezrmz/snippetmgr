using System.Threading.Tasks;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.Interfaces;

public interface IImportService
{
    Task<ImportResult> ImportFromDirectoryAsync(string directoryPath);
}
