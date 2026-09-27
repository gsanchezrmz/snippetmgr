using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FuzzySharp;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Repositories;

public class LocalSnippetRepository : ISnippetRepository
{
    private readonly IStorageService _storageService;

    public LocalSnippetRepository(IStorageService storageService)
    {
        _storageService = storageService;
        _storageService.EnsureStorageDirectoryExists();
    }

    public async Task<IEnumerable<Snippet>> GetAllSnippetsAsync()
    {
        var directory = _storageService.GetStorageDirectory();
        var files = Directory.GetFiles(directory, "*.json");
        var snippets = new List<Snippet>();

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, Encoding.UTF8);
                var snippet = JsonSerializer.Deserialize<Snippet>(content);
                if (snippet != null)
                {
                    snippets.Add(snippet);
                }
            }
            catch
            {
                // Ignore malformed files
            }
        }

        return snippets;
    }

    public async Task SaveSnippetAsync(Snippet snippet)
    {
        var directory = _storageService.GetStorageDirectory();
        _storageService.EnsureStorageDirectoryExists();

        var filePath = Path.Combine(directory, $"{snippet.Id}.json");
        var content = JsonSerializer.Serialize(snippet, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(false)); // strict UTF-8 without BOM
    }

    public Task DeleteSnippetAsync(string id)
    {
        var directory = _storageService.GetStorageDirectory();
        var filePath = Path.Combine(directory, $"{id}.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Snippet>> SearchSnippetsAsync(string query)
    {
        var allSnippets = await GetAllSnippetsAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            return allSnippets;
        }

        var results = allSnippets.Select(s => new
        {
            Snippet = s,
            Score = Fuzz.PartialRatio(query.ToLowerInvariant(), $"{s.Title} {s.Description} {s.Tags}".ToLowerInvariant())
        })
        .Where(x => x.Score > 50)
        .OrderByDescending(x => x.Score)
        .Select(x => x.Snippet);

        return results;
    }
}
