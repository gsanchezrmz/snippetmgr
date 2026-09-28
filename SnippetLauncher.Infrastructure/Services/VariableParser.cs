using System;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class VariableParser : IVariableParser
{
    public string Parse(string template, string currentClipboard)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var result = template.Replace("$DATE", DateTime.Now.ToString("yyyy-MM-dd"));
        result = result.Replace("$CLIPBOARD", currentClipboard ?? string.Empty);

        return result;
    }
}
