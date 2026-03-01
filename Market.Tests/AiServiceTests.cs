using System.Net;
using System.Text.Json;
using FluentAssertions;
using Market.Web.Core.DTOs;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Options;
using Market.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Market.Tests;

[TestFixture]
public class AiServiceTests
{
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private Mock<IOptions<OpenRouterOptions>> _optionsMock;
    private Mock<ILogger<OpenRouterAiService>> _loggerMock;
    private OpenRouterOptions _options;

    [SetUp]
    public void Setup()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://openrouter.ai/")
        };

        _options = new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            Referer = "http://test-referer.com",
            AppTitle = "TestApp"
        };

        _optionsMock = new Mock<IOptions<OpenRouterOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);

        _loggerMock = new Mock<ILogger<OpenRouterAiService>>();

        string promptDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts");
        Directory.CreateDirectory(promptDir);
        File.WriteAllText(Path.Combine(promptDir, "system_prompt.txt"), "Test prompt");
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    private OpenRouterAiService CreateService() => 
        new OpenRouterAiService(_httpClient, _optionsMock.Object, _loggerMock.Object);

    private List<IFormFile> CreateDummyFile()
    {
        var fileMock = new Mock<IFormFile>();
        var content = "dummy file data"u8.ToArray();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((stream, _) => stream.Write(content, 0, content.Length))
            .Returns(Task.CompletedTask);

        return new List<IFormFile> { fileMock.Object };
    }

    [Test]
    public async Task GenerateFromImagesAsync_ShouldReturnDto_WhenResponseIsValid()
    {
        // Arrange
        var service = CreateService();
        var files = CreateDummyFile();

        var fakeResponse = new {
            choices = new[] { new { message = new { content = "{\"Title\":\"Test\",\"Description\":\"Desc\",\"SuggestedPrice\":100,\"Category\":\"Inne\"}" } } }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(fakeResponse))
            });

        // Act
        var result = await service.GenerateFromImagesAsync(files);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test");
        result.SuggestedPrice.Should().Be(100);
    }

    [Test]
    public void GenerateFromImagesAsync_ShouldThrowAiGenerationException_WhenApiReturnsNonSuccess()
    {
        // Arrange
        var service = CreateService();
        var files = CreateDummyFile();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable, Content = new StringContent("Server busy") });

        // Act
        Func<Task> action = async () => await service.GenerateFromImagesAsync(files);

        // Assert
        action.Should().ThrowAsync<AiGenerationException>().WithMessage("*OpenRouter API Error*");
    }

    [Test]
    public void GenerateFromImagesAsync_ShouldThrowAiGenerationException_WhenResponseIsMalformedJson()
    {
        // Arrange
        var service = CreateService();
        var files = CreateDummyFile();

        var fakeResponse = new { choices = new[] { new { message = new { content = "Not JSON format" } } } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonSerializer.Serialize(fakeResponse)) });

        // Act
        Func<Task> action = async () => await service.GenerateFromImagesAsync(files);

        // Assert
        action.Should().ThrowAsync<AiGenerationException>().WithMessage("*Błąd parsowania JSON z AI*");
    }
}
