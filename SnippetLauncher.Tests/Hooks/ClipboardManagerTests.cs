using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SnippetLauncher.Core.Models;
using SnippetLauncher.Infrastructure.Services;
using Xunit;

namespace SnippetLauncher.Tests.Hooks;

public class ClipboardManagerTests
{
    [Fact]
    public async Task ClipboardManager_InjectSnippet_RestoresPreviousClipboardState()
    {
        // Arrange
        var mockParser = new Mock<SnippetLauncher.Core.Interfaces.IVariableParser>();
        mockParser.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>())).Returns("Parsed Snippet");

        var mockClipboard = new Mock<ISystemClipboard>();
        var clipboardState = "Original Clipboard Content";

        mockClipboard.Setup(c => c.ContainsText()).Returns(true);
        mockClipboard.Setup(c => c.GetText()).Returns(() => clipboardState);
        mockClipboard.Setup(c => c.SetText(It.IsAny<string>())).Callback<string>(s => clipboardState = s);

        var manager = new ClipboardManager(mockParser.Object, mockClipboard.Object);
        var snippet = new Snippet { Code = "Some Snippet" };

        // Act
        await manager.InjectSnippetAsync(snippet);

        // Assert
        mockClipboard.Verify(c => c.SimulatePaste(), Times.Once);
        clipboardState.Should().Be("Original Clipboard Content");
    }

    [Fact]
    public async Task ClipboardManager_InjectSnippet_WithNoText_ClearsClipboardAfter()
    {
        // Arrange
        var mockParser = new Mock<SnippetLauncher.Core.Interfaces.IVariableParser>();
        mockParser.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>())).Returns("Parsed Snippet");

        var mockClipboard = new Mock<ISystemClipboard>();
        var clipboardState = "Image Data"; // mock non-text state

        mockClipboard.Setup(c => c.ContainsText()).Returns(false);
        mockClipboard.Setup(c => c.GetText()).Returns(() => clipboardState);
        mockClipboard.Setup(c => c.SetText(It.IsAny<string>())).Callback<string>(s => clipboardState = s);

        var manager = new ClipboardManager(mockParser.Object, mockClipboard.Object);
        var snippet = new Snippet { Code = "Some Snippet" };

        // Act
        await manager.InjectSnippetAsync(snippet);

        // Assert
        mockClipboard.Verify(c => c.SimulatePaste(), Times.Once);
        clipboardState.Should().Be(string.Empty);
    }
}
