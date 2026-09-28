using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Core.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISnippetRepository _repository;
    private readonly IClipboardManager _clipboardManager;

    public MainViewModel(ISnippetRepository repository, IClipboardManager clipboardManager)
    {
        _repository = repository;
        _clipboardManager = clipboardManager;
        Snippets = new ObservableCollection<Snippet>();
    }

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Snippet? _selectedSnippet;

    public ObservableCollection<Snippet> Snippets { get; }

    public Action? HideWindowAction { get; set; }

    partial void OnSearchQueryChanged(string value)
    {
        _ = PerformSearchAsync(value);
    }

    private async Task PerformSearchAsync(string query)
    {
        var results = await _repository.SearchSnippetsAsync(query);

        Snippets.Clear();
        foreach (var snippet in results)
        {
            Snippets.Add(snippet);
        }

        SelectedSnippet = Snippets.FirstOrDefault();
    }

    [RelayCommand]
    private async Task InjectSnippetAsync()
    {
        if (SelectedSnippet != null)
        {
            HideWindowAction?.Invoke();
            await _clipboardManager.InjectSnippetAsync(SelectedSnippet);
        }
    }

    public async Task InitializeAsync()
    {
        await PerformSearchAsync(string.Empty);
    }
}
