using Microsoft.AspNetCore.Http;
using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Tests.Unit;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit;

public class ProjectImageServiceTests : BaseServiceTests, IAsyncLifetime
{
    private readonly ProjectImageService _projectImageService;
    private readonly Project _project;
    
    public ProjectImageServiceTests()
    {
        _project = CreateProject(1);
        
        var mockFileService = new Mock<IFileService>();
        _projectImageService = new ProjectImageService(Context, mockFileService.Object);
        
        mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(() => "testPath");
    }
    
    public async Task InitializeAsync()
    {
        await ClearDatabase();
        await SeedDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task GetProjectImagesAsync_ValidProjectId_ReturnsProjectImages()
    {
        // Act
        var result = await _projectImageService.GetProjectImagesAsync(_project.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Count());
    }
    
    [Fact]
    public async Task GetProjectImagesAsync_InvalidProjectId_ThrowsNotFoundException()
    {
        // Arrange
        const int projectId = 99;
        
        // Act
        async Task Act() => await _projectImageService.GetProjectImagesAsync(projectId);
        
        // Assert
        await Assert.ThrowsAsync<NotFoundException>(Act);
    }
    
    [Fact]
    public async Task GetProjectImageAsync_ValidProjectIdAndImageId_ReturnsProjectImage()
    {
        // Arrange
        const int imageId = 1;
        
        // Act
        var result = await _projectImageService.GetProjectImageAsync(_project.Id, imageId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(imageId, result.Id);
    }
    
    [Fact]
    public async Task GetProjectImageAsync_InvalidProjectId_ThrowsNotFoundException()
    {
        // Arrange
        const int projectId = 99;
        const int imageId = 1;
        
        // Act
        async Task Act() => await _projectImageService.GetProjectImageAsync(projectId, imageId);
        
        // Assert
        await Assert.ThrowsAsync<NotFoundException>(Act);
    }
    
    [Fact]
    public async Task GetProjectImageAsync_InvalidImageId_ThrowsNotFoundException()
    {
        // Arrange
        const int imageId = 99;
        
        // Act
        async Task Act() => await _projectImageService.GetProjectImageAsync(_project.Id, imageId);
        
        // Assert
        await Assert.ThrowsAsync<NotFoundException>(Act);
    }
    
    [Fact]
    public async Task SyncProjectImagesAsync_ValidProjectIdAndImages_ReturnsProjectImages()
    {
        // Arrange
        var images = new List<CreateProjectImage>
        {
            new() {Id = 1, Type = ImageType.Logo},
            new() {Id = 2, Type = ImageType.Banner},
            new() {Id = 3, Type = ImageType.Card},
            new() {Id = 4, Type = ImageType.Desktop, Order = 1},
            new() {Id = 6, Type = ImageType.Mobile, Order = 1},
            new() {Image = null, Type = ImageType.Desktop, Order = 2},
            new() {Image = null, Type = ImageType.Desktop, Order = 3},
            new() {Image = null, Type = ImageType.Mobile, Order = 2},
            new() {Image = null, Type = ImageType.Mobile, Order = 3}
        };
        
        // Act
        var result = await _projectImageService.SyncProjectImagesAsync(_project, images);
        
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
            new() {Id = 1, Type = ImageType.Logo},
            new() {Id = 2, Type = ImageType.Banner},
            new() {Id = 3, Type = ImageType.Card},
            new() {Id = 4, Type = ImageType.Desktop, Order = 1},
            new() {Id = 6, Type = ImageType.Mobile, Order = 1},
            new() {Image = null, Type = ImageType.Desktop, Order = 2},
            new() {Image = null, Type = ImageType.Desktop, Order = 3},
            new() {Image = null, Type = ImageType.Mobile, Order = 2},
            new() {Image = null, Type = ImageType.Mobile, Order = 3}
        };
        
        // Act
        await _projectImageService.SyncProjectImagesAsync(_project, images);
        var result = await _projectImageService.GetProjectImagesAsync(_project.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Count());
    }

    private async Task SeedDatabase()
    {
        if (!Context.ProjectImage.Any())
        {
            Context.ProjectImage.AddRange(GetTestProjectImages());
            await Context.SaveChangesAsync();
        }
    }
    
    private IEnumerable<ProjectImage> GetTestProjectImages()
    {
        var testLogo = CreateProjectImage(1, "testLogo.png", ImageType.Logo, project: _project);
        var testBanner = CreateProjectImage(2, "testBanner.png", ImageType.Banner, project: _project);
        var testCard = CreateProjectImage(3, "testCard.png", ImageType.Card, project: _project);
        var testDesktop1 = CreateProjectImage(4, "testDesktop1.png", ImageType.Desktop, order: 1, project: _project);
        var testDesktop2 = CreateProjectImage(5, "testDesktop2.png", ImageType.Desktop, order: 2, project: _project);
        var testMobile1 = CreateProjectImage(6, "testMobile1.png", ImageType.Mobile, order: 1, project: _project);
        var testMobile2 = CreateProjectImage(7, "testMobile2.png", ImageType.Mobile, order: 2, project: _project);
        
        return [
            testLogo,
            testBanner,
            testCard,
            testDesktop1,
            testDesktop2,
            testMobile1,
            testMobile2
        ];
    }
}