using System;
using System.IO;
using System.Text.Json;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "SnippetLauncher");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public Settings GetSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaultSettings = new Settings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
    }

    public void SaveSettings(Settings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
