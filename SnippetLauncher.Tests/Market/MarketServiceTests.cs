using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using SnippetLauncher.Infrastructure.Market;
using Xunit;

namespace SnippetLauncher.Tests.Market;

public class MarketServiceTests
{
    [Fact]
    public async Task Market_DownloadSnippet_RejectsInvalidJsonSchema()
    {
        // Arrange
        var invalidJson = @"{ ""Title"": """", ""Description"": ""No content"" }"; // Missing required Id, Content. Title empty
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(invalidJson)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var marketService = new MarketService(httpClient);

        // Act & Assert
        Func<Task> action = async () => await marketService.DownloadSnippetAsync("http://dummy.market/snippet.json");
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid snippet schema.");
    }

    [Fact]
    public async Task Market_DownloadSnippet_AcceptsValidSchema()
    {
        // Arrange
        var validJson = @"{ ""Id"": ""123"", ""Title"": ""Test"", ""Content"": ""Code"" }";
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(validJson)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var marketService = new MarketService(httpClient);

        // Act
        var snippet = await marketService.DownloadSnippetAsync("http://dummy.market/snippet.json");

        // Assert
        snippet.Should().NotBeNull();
        snippet.Title.Should().Be("Test");
    }
}
