using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Services;

public class SmartImportService : IImportService
{
    private readonly ISnippetRepository _repository;

    public SmartImportService(ISnippetRepository repository)
    {
        _repository = repository;
    }

    public async Task<ImportResult> ImportFromDirectoryAsync(string directoryPath)
    {
        var result = new ImportResult();
        if (!Directory.Exists(directoryPath)) return result;

        var existingSnippets = (await _repository.GetAllSnippetsAsync()).ToList();

        // 1. VS Code Scan
        await ScanVSCodeExtensionAsync(directoryPath, existingSnippets, result);

        // 2. Local Folder Scan (Plain text files)
        await ScanLocalFoldersAsync(directoryPath, existingSnippets, result);

        return result;
    }

    private async Task ScanVSCodeExtensionAsync(string directoryPath, List<Snippet> existingSnippets, ImportResult result)
    {
        var packageJsonFiles = Directory.GetFiles(directoryPath, "package.json", SearchOption.AllDirectories);

        foreach (var packageFile in packageJsonFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(packageFile);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("contributes", out var contributes) &&
                    contributes.TryGetProperty("snippets", out var snippetsNode) &&
                    snippetsNode.ValueKind == JsonValueKind.Array)
                {
                    var packageDir = Path.GetDirectoryName(packageFile) ?? directoryPath;

                    foreach (var snippetEntry in snippetsNode.EnumerateArray())
                    {
                        var language = snippetEntry.TryGetProperty("language", out var langNode) ? langNode.GetString() ?? "" : "";
                        var path = snippetEntry.TryGetProperty("path", out var pathNode) ? pathNode.GetString() ?? "" : "";

                        if (!string.IsNullOrEmpty(path))
                        {
                            var snippetFilePath = Path.Combine(packageDir, path);
                            if (File.Exists(snippetFilePath))
                            {
                                await ExtractVSCodeSnippetFileAsync(snippetFilePath, language, existingSnippets, result);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    private async Task ExtractVSCodeSnippetFileAsync(string filePath, string language, List<Snippet> existingSnippets, ImportResult result)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            using var doc = JsonDocument.Parse(content);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var name = prop.Name;
                var detail = prop.Value;

                string prefix = detail.TryGetProperty("prefix", out var pElement) ? pElement.GetString() ?? "" : "";
                string description = detail.TryGetProperty("description", out var dElement) ? dElement.GetString() ?? "" : "";

                string bodyStr = "";
                if (detail.TryGetProperty("body", out var bElement))
                {
                    if (bElement.ValueKind == JsonValueKind.Array)
                    {
                        var lines = bElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                        bodyStr = string.Join(Environment.NewLine, lines);
                    }
                    else if (bElement.ValueKind == JsonValueKind.String)
                    {
                        bodyStr = bElement.GetString() ?? "";
                    }
                }

                if (!string.IsNullOrEmpty(bodyStr))
                {
                    var title = string.IsNullOrEmpty(name) ? prefix : name;
                    await UpsertSnippetAsync(title, bodyStr, language, prefix, filePath, existingSnippets, result);
                }
            }
        }
        catch { }
    }

    private async Task ScanLocalFoldersAsync(string directoryPath, List<Snippet> existingSnippets, ImportResult result)
    {
        var allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in allFiles)
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            // Ignore common non-text or system files
            if (extension == ".db" || extension == ".json" || extension == ".dll" || extension == ".exe" || extension == ".zip" || extension == ".vsix" || file.Contains(".git"))
            {
                continue;
            }

            try
            {
                var dirName = new DirectoryInfo(Path.GetDirectoryName(file) ?? "").Name;
                var language = dirName.ToLowerInvariant(); // Infer language from parent folder
                var title = Path.GetFileNameWithoutExtension(file);
                var code = await File.ReadAllTextAsync(file);

                if (!string.IsNullOrWhiteSpace(code))
                {
                    await UpsertSnippetAsync(title, code.Trim(), language, language, file, existingSnippets, result);
                }
            }
            catch { }
        }
    }

    private async Task UpsertSnippetAsync(string title, string code, string language, string tags, string sourcePath, List<Snippet> existingSnippets, ImportResult result)
    {
        result.Found++;

        var isDuplicate = existingSnippets.Any(s =>
            string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.Code, code, StringComparison.Ordinal));

        if (isDuplicate)
        {
            result.Ignored++;
        }
        else
        {
            var snippet = new Snippet
            {
                Title = title,
                Code = code,
                Language = language,
                Tags = tags,
                SourcePath = sourcePath
            };

            await _repository.SaveSnippetAsync(snippet);
            existingSnippets.Add(snippet); // Update cache for current run
            result.New++;
        }
    }
}
