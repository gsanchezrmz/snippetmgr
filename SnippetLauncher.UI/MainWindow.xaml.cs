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

    public MainWindow()
    {
        InitializeComponent();
        _hotkeyService = new HotkeyService();
        _settingsService = new SettingsService();
    }

    public MainWindow(ISettingsService settingsService, HotkeyService hotkeyService, ISnippetRepository repository)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;

        var settings = _settingsService.GetSettings();

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
        if (DataContext is MainViewModel vm)
        {
            _ = vm.InitializeAsync(); // Refresh in case settings modified
        }
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
