using Microsoft.AspNetCore.Http;
using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Services.Image;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class ProjectImageServiceTests : BaseServiceTests
{
    private static readonly Guid ProjectId = Project1Id;
    private readonly ProjectImageService _projectImageService;

    public ProjectImageServiceTests()
    {
        var mockFileService = new Mock<IFileService>();
        var mockImageService = new Mock<IImageService>();
        _projectImageService = new ProjectImageService(Context, mockFileService.Object, mockImageService.Object);

        mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(() => new Success<string>("testPath"));
        mockImageService.Setup(s => s.OptimiseImageAsync(It.IsAny<byte[]>()))
            .ReturnsAsync(() => new Success<byte[]>(new byte[12]));
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
        var invalidProjectId = Guid.NewGuid();

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
        var imageId = Image1Id;

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
        var invalidProjectId = Guid.NewGuid();
        var imageId = Image1Id;

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
        var invalidImageId = Guid.NewGuid();

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
            new() { Id = Image1Id, Type = ImageType.Logo },
            new() { Id = Image2Id, Type = ImageType.Banner },
            new() { Id = Image3Id, Type = ImageType.Card },
            new() { Id = Image4Id, Type = ImageType.Desktop, Order = 1 },
            new() { Id = Image6Id, Type = ImageType.Mobile, Order = 1 },
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
            new() { Id = Image1Id, Type = ImageType.Logo },
            new() { Id = Image2Id, Type = ImageType.Banner },
            new() { Id = Image3Id, Type = ImageType.Card },
            new() { Id = Image4Id, Type = ImageType.Desktop, Order = 1 },
            new() { Id = Image6Id, Type = ImageType.Mobile, Order = 1 },
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
