using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using SnippetLauncher.Core.ViewModels;

namespace SnippetLauncher.UI;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;

    // Parameterless constructor needed for XAML designer (if strictly needed, though we can omit if careful)
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(ISettingsService settingsService, HotkeyService hotkeyService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;

        var settings = _settingsService.GetSettings();

        // Custom storage if path configured, otherwise default
        var storage = new LocalStorageService(settings.SnippetsPath);
        var repository = new LocalSnippetRepository(storage);
        var clipboardManager = new ClipboardManager(new VariableParser(), new SystemClipboard());

        var viewModel = new MainViewModel(repository, clipboardManager)
        {
            HideWindowAction = Hide
        };

        DataContext = viewModel;

        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();

        var source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(_hotkeyService.ProcessMessage);

        if (settings.IsHotkeyConfigured)
        {
            _hotkeyService.TryRegister(helper.Handle, ShowWindow, settings.HotkeyModifiers, settings.HotkeyKey);
        }

        _ = viewModel.InitializeAsync();

        Closing += (s, e) => {
            e.Cancel = true;
            Hide();
        };

        Closed += (s, e) => {
            _hotkeyService.UnregisterGlobalHotkey();
        };
    }

    public void ShowWindow()
    {
        Show();
        Activate();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Down && SearchBox.IsFocused)
        {
            SnippetList.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel vm && vm.InjectSnippetCommand.CanExecute(null))
            {
                vm.InjectSnippetCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
