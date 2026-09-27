using System;
using FluentAssertions;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Parser;

public class VariableParserTests
{
    [Fact]
    public void VariablesParser_InjectsDateAndClipboard_Correctly()
    {
        // Arrange
        var parser = new VariableParser();
        var template = "Date: $DATE, Content: $CLIPBOARD";
        var clipboardContent = "Sample Clipboard Data";

        // Act
        var result = parser.Parse(template, clipboardContent);

        // Assert
        result.Should().Contain(DateTime.Now.ToString("yyyy-MM-dd"));
        result.Should().Contain("Content: Sample Clipboard Data");
    }
}
