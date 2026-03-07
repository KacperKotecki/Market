using FluentAssertions;
using Market.Web.Core.Exceptions;
using Market.Web.Services.AI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Market.Tests;

[TestFixture]
public class PromptProviderTests
{
    private Mock<IWebHostEnvironment> _envMock;
    private Mock<ILogger<PromptProvider>> _loggerMock;
    private string _tempRoot;

    [SetUp]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Prompts"));

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);

        _loggerMock = new Mock<ILogger<PromptProvider>>();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private PromptProvider CreateService() =>
        new PromptProvider(_envMock.Object, _loggerMock.Object);

    [Test]
    public async Task GetSystemPromptAsync_ShouldReturnPromptContent_WhenFileExists()
    {
        // Arrange
        var promptPath = Path.Combine(_tempRoot, "Prompts", "system_prompt.txt");
        await File.WriteAllTextAsync(promptPath, "You are a helpful assistant.");
        var provider = CreateService();

        // Act
        var result = await provider.GetSystemPromptAsync();

        // Assert
        result.Should().Be("You are a helpful assistant.");
    }

    [Test]
    public void GetSystemPromptAsync_ShouldThrowAiGenerationException_WhenFileDoesNotExist()
    {
        // Arrange — prompt file is never created in temp dir
        var provider = CreateService();

        // Act
        Func<Task> action = async () => await provider.GetSystemPromptAsync();

        // Assert
        action.Should().ThrowAsync<AiGenerationException>()
            .WithMessage("*Nie można znaleźć lub odczytać pliku z promptem systemowym*");
    }

    [Test]
    public void GetSystemPromptAsync_ShouldThrowAiGenerationException_WhenFileIsEmpty()
    {
        // Arrange
        var promptPath = Path.Combine(_tempRoot, "Prompts", "system_prompt.txt");
        File.WriteAllText(promptPath, string.Empty);
        var provider = CreateService();

        // Act
        Func<Task> action = async () => await provider.GetSystemPromptAsync();

        // Assert
        action.Should().ThrowAsync<AiGenerationException>()
            .WithMessage("*Nie można znaleźć lub odczytać pliku z promptem systemowym*");
    }
}
