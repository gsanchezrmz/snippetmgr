using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Services;
using Forms = System.Windows.Forms;

namespace SnippetLauncher.UI;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly HotkeyService _hotkeyService;
    private readonly IImportService _importService;
    private readonly ISnippetRepository _repository;
    private readonly IntPtr _windowHandle;

    private string _currentModifiers = "";
    private string _currentKey = "";

    public ObservableCollection<Snippet> Snippets { get; set; } = new();
    public ObservableCollection<string> Languages { get; set; } = new();

    public SettingsWindow(ISettingsService settingsService, HotkeyService hotkeyService, IImportService importService, ISnippetRepository repository, IntPtr windowHandle)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _importService = importService;
        _repository = repository;
        _windowHandle = windowHandle;

        var languages = new[] { "C#", "CSS", "javascript", "json", "powershell", "python", "sql", "pandas", "pyspark", "databricks", "xml", "yaml", "typescript", "odata" };
        foreach (var l in languages) Languages.Add(l);

        SnippetsDataGrid.ItemsSource = Snippets;
        DataContext = this;

        LoadSettingsAndData();
    }

    private async void LoadSettingsAndData()
    {
        var settings = _settingsService.GetSettings();
        _currentModifiers = settings.HotkeyModifiers;
        _currentKey = settings.HotkeyKey;

        UpdateHotkeyDisplay();

        var snippets = await _repository.GetAllSnippetsAsync();
        Snippets.Clear();
        foreach (var s in snippets)
        {
            Snippets.Add(s);
        }
    }

    private void UpdateHotkeyDisplay()
    {
        if (string.IsNullOrEmpty(_currentKey))
        {
            HotkeyTextBox.Text = "Not configured";
        }
        else
        {
            HotkeyTextBox.Text = string.IsNullOrEmpty(_currentModifiers) ? _currentKey : $"{_currentModifiers}, {_currentKey}";
        }
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = (e.Key == Key.System ? e.SystemKey : e.Key);

        // Ignore standalone modifier keys
        if (key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var modStrings = new System.Collections.Generic.List<string>();

        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) modStrings.Add("Control");
        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) modStrings.Add("Alt");
        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) modStrings.Add("Shift");
        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) modStrings.Add("Win");

        _currentModifiers = string.Join(", ", modStrings);
        _currentKey = key.ToString();

        UpdateHotkeyDisplay();
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog();
        dialog.Description = "Select a folder to import snippets from (VS Code or Local format)";

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            var result = await _importService.ImportFromDirectoryAsync(dialog.SelectedPath);
            System.Windows.MessageBox.Show($"Import Complete\n\nFound: {result.Found}\nNew: {result.New}\nIgnored (Duplicates): {result.Ignored}", "Smart Import", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            // Reload grid
            LoadSettingsAndData();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.GetSettings();
        settings.HotkeyModifiers = _currentModifiers;
        settings.HotkeyKey = _currentKey;

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
            _hotkeyService.UnregisterGlobalHotkey();
        }

        _settingsService.SaveSettings(settings);

        // Save Snippets
        var existing = await _repository.GetAllSnippetsAsync();

        // Delete removed
        var currentIds = Snippets.Select(s => s.Id).ToList();
        var toDelete = existing.Where(e => !currentIds.Contains(e.Id));
        foreach (var del in toDelete)
        {
            await _repository.DeleteSnippetAsync(del.Id);
        }

        // Upsert modified or new
        foreach (var snippet in Snippets)
        {
            await _repository.SaveSnippetAsync(snippet);
        }

        System.Windows.MessageBox.Show("Settings and Snippets saved! The application must be restarted to apply hotkey changes.", "Restart Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
