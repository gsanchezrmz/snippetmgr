namespace SnippetLauncher.Core.Models;

public class Settings
{
    public string SnippetsPath { get; set; } = string.Empty;
    public string HotkeyModifiers { get; set; } = "Control, Alt";
    public string HotkeyKey { get; set; } = "Space";
    public bool IsHotkeyConfigured => !string.IsNullOrEmpty(HotkeyKey);
}
