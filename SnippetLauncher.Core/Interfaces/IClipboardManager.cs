using System.Threading.Tasks;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.Interfaces;

public interface IClipboardManager
{
    Task InjectSnippetAsync(Snippet snippet);
}
