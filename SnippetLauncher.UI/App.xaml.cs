using System;
using System.Windows;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using Forms = System.Windows.Forms;

namespace SnippetLauncher.UI;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private readonly SettingsService _settingsService = new();
    private readonly HotkeyService _hotkeyService = new();
    private SQLiteSnippetRepository? _repository;
    private SmartImportService? _importService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = _settingsService.GetSettings();

        var storage = new LocalStorageService(settings.SnippetsPath);
        _repository = new SQLiteSnippetRepository(storage);
        _importService = new SmartImportService(_repository);

        _mainWindow = new MainWindow(_settingsService, _hotkeyService, _repository);

        _notifyIcon = new Forms.NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Snippet Launcher";

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Settings", null, (s, ev) => ShowSettings());
        contextMenu.Items.Add("Exit", null, (s, ev) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, ev) => _mainWindow.ShowWindow();

        if (!settings.IsHotkeyConfigured)
        {
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        if (_mainWindow != null && _repository != null && _importService != null)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(_mainWindow);
            var settingsWindow = new SettingsWindow(_settingsService, _hotkeyService, _importService, _repository, helper.Handle);
            settingsWindow.ShowDialog();
        }
    }

    private void ExitApplication()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        Shutdown();
    }
}
