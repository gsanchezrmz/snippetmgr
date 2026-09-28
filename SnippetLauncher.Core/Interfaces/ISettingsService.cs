using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.Interfaces;

public interface ISettingsService
{
    Settings GetSettings();
    void SaveSettings(Settings settings);
}
