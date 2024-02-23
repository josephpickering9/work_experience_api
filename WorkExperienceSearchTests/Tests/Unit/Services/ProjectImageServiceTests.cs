using Microsoft.AspNetCore.Http;
using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class ProjectImageServiceTests : BaseServiceTests
{
    private const int ProjectId = 1;
    private readonly ProjectImageService _projectImageService;

    public ProjectImageServiceTests()
    {
        var mockFileService = new Mock<IFileService>();
        _projectImageService = new ProjectImageService(Context, mockFileService.Object);

        mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(() => new Success<string>("testPath"));
    }

    [Fact]
    public async Task GetProjectImagesAsync_ValidProjectId_ReturnsProjectImages()
    {
        // Act
        var result = (await _projectImageService.GetProjectImagesAsync(ProjectId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Count());
    }

    [Fact]
    public async Task GetProjectImagesAsync_InvalidProjectId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int invalidProjectId = 99;

        // Act
        var result = (await _projectImageService.GetProjectImagesAsync(invalidProjectId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Project not found.", result.Message);
    }

    [Fact]
    public async Task GetProjectImageAsync_ValidProjectIdAndImageId_ReturnsProjectImage()
    {
        // Arrange
        const int imageId = 1;

        // Act
        var result = (await _projectImageService.GetProjectImageAsync(ProjectId, imageId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(imageId, result.Id);
    }

    [Fact]
    public async Task GetProjectImageAsync_InvalidProjectId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int invalidProjectId = 99;
        const int imageId = 1;

        // Act
        var result = (await _projectImageService.GetProjectImageAsync(invalidProjectId, imageId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Project not found.", result.Message);
    }

    [Fact]
    public async Task GetProjectImageAsync_InvalidImageId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int invalidImageId = 99;

        // Act
        var result = (await _projectImageService.GetProjectImageAsync(ProjectId, invalidImageId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Image not found.", result.Message);
    }

    [Fact]
    public async Task SyncProjectImagesAsync_ValidProjectIdAndImages_ReturnsProjectImages()
    {
        // Arrange
        var images = new List<CreateProjectImage>
        {
            new() { Id = 1, Type = ImageType.Logo },
            new() { Id = 2, Type = ImageType.Banner },
            new() { Id = 3, Type = ImageType.Card },
            new() { Id = 4, Type = ImageType.Desktop, Order = 1 },
            new() { Id = 6, Type = ImageType.Mobile, Order = 1 },
            new() { Image = null, Type = ImageType.Desktop, Order = 2 },
            new() { Image = null, Type = ImageType.Desktop, Order = 3 },
            new() { Image = null, Type = ImageType.Mobile, Order = 2 },
            new() { Image = null, Type = ImageType.Mobile, Order = 3 }
        };

        // Act
        var result = (await _projectImageService.SyncProjectImagesAsync(ProjectId, images)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Count);
    }

    [Fact]
    public async Task SyncProjectImagesAsync_ValidProjectIdAndImages_DeletesProjectImages()
    {
        // Arrange
        var images = new List<CreateProjectImage>
        {
            new() { Id = 1, Type = ImageType.Logo },
            new() { Id = 2, Type = ImageType.Banner },
            new() { Id = 3, Type = ImageType.Card },
            new() { Id = 4, Type = ImageType.Desktop, Order = 1 },
            new() { Id = 6, Type = ImageType.Mobile, Order = 1 },
            new() { Image = null, Type = ImageType.Desktop, Order = 2 },
            new() { Image = null, Type = ImageType.Desktop, Order = 3 },
            new() { Image = null, Type = ImageType.Mobile, Order = 2 },
            new() { Image = null, Type = ImageType.Mobile, Order = 3 }
        };

        // Act
        await _projectImageService.SyncProjectImagesAsync(ProjectId, images);
        var result = (await _projectImageService.GetProjectImagesAsync(ProjectId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Count());
    }
}