using System;

namespace SnippetLauncher.Core.Models;

public class Snippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
}
