namespace SnippetLauncher.Core.Interfaces;

public interface IVariableParser
{
    string Parse(string template, string currentClipboard);
}
