using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SnippetLauncher.Infrastructure.Repositories;
using SnippetLauncher.Infrastructure.Services;
using SnippetLauncher.UI.ViewModels;

namespace SnippetLauncher.UI;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();

        var storage = new LocalStorageService();
        var repository = new LocalSnippetRepository(storage);
        var clipboardManager = new ClipboardManager(new VariableParser(), new SystemClipboard());

        var viewModel = new MainViewModel(repository, clipboardManager)
        {
            HideWindowAction = Hide
        };

        DataContext = viewModel;
        _hotkeyService = new HotkeyService();

        Loaded += async (s, e) => {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(_hotkeyService.ProcessMessage);
            _hotkeyService.Register(source!.Handle, ShowWindow);

            SearchBox.Focus();
            await viewModel.InitializeAsync();
        };

        Closed += (s, e) => {
            _hotkeyService.UnregisterGlobalHotkey();
        };
    }

    private void ShowWindow()
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

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
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
