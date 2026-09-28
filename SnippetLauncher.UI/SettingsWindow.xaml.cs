using System;
using System.Windows;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Services;

namespace SnippetLauncher.UI;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly HotkeyService _hotkeyService;
    private readonly IntPtr _windowHandle;

    public SettingsWindow(ISettingsService settingsService, HotkeyService hotkeyService, IntPtr windowHandle)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _windowHandle = windowHandle;

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.GetSettings();
        SnippetsPathTextBox.Text = settings.SnippetsPath;
        ModifiersTextBox.Text = settings.HotkeyModifiers;
        KeyTextBox.Text = settings.HotkeyKey;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.GetSettings();
        settings.SnippetsPath = SnippetsPathTextBox.Text;
        settings.HotkeyModifiers = ModifiersTextBox.Text;
        settings.HotkeyKey = KeyTextBox.Text;

        // Try registering to see if there is a collision
        bool registered = _hotkeyService.TryRegister(_windowHandle, () => { }, settings.HotkeyModifiers, settings.HotkeyKey);

        if (!registered)
        {
            var result = System.Windows.MessageBox.Show(
                "The selected hotkey is already in use by another application. Do you want to force save anyway? (You may need to change it later)",
                "Hotkey Collision",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.No)
            {
                return;
            }
        }
        else
        {
            // Unregister our test hook immediately
            _hotkeyService.UnregisterGlobalHotkey();
        }

        _settingsService.SaveSettings(settings);

        System.Windows.MessageBox.Show("Settings saved! The application must be restarted to apply these changes.", "Restart Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
