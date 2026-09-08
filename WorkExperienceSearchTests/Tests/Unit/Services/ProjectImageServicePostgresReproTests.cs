using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Work_Experience_Search.Models;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Services;
using Work_Experience_Search.Services.Image;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class ProjectImageServicePostgresReproTests : IAsyncLifetime
{
    private Database _context = null!;
    private ProjectId _projectId;
    private ProjectImageId _logoId;
    private ProjectImageId _bannerId;
    private ProjectImageId _cardId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseNpgsql("Host=localhost;Port=5433;Database=testdb;Username=testuser;Password=testpassword")
            .Options;

        _context = new Database(options);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();

        _projectId = ProjectId.New();
        _logoId = ProjectImageId.New();
        _bannerId = ProjectImageId.New();
        _cardId = ProjectImageId.New();

        var project = new Project
        {
            Id = _projectId,
            Title = "Postgres Repro Project",
            Description = "Description",
            ShortDescription = "Short Description",
            StartDate = new DateOnly(2020, 1, 1),
            Slug = "postgres-repro-project"
        };

        _context.Project.Add(project);
        _context.ProjectImage.AddRange(
            new ProjectImage { Id = _logoId, Image = "originalLogo.png", Type = ImageType.Logo, ProjectId = _projectId },
            new ProjectImage { Id = _bannerId, Image = "originalBanner.png", Type = ImageType.Banner, ProjectId = _projectId },
            new ProjectImage { Id = _cardId, Image = "originalCard.png", Type = ImageType.Card, ProjectId = _projectId }
        );
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SyncProjectImagesAsync_ReplacingBannerWithNewUpload_AgainstRealPostgres_LeavesCardUnaffected()
    {
        var mockFileService = new Mock<IFileService>();
        var mockImageService = new Mock<IImageService>();
        mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(() => new Success<string>("newBanner.png"));
        mockImageService.Setup(s => s.OptimiseImageAsync(It.IsAny<byte[]>())).ReturnsAsync(() => new Success<byte[]>(new byte[12]));

        var projectRepository = new Work_Experience_Search.Repositories.ProjectRepository(_context, new MemoryCache(new MemoryCacheOptions()), new CacheInvalidator());
        var projectImageRepository = new ProjectImageRepository(_context);
        var service = new ProjectImageService(projectRepository, projectImageRepository, mockFileService.Object, mockImageService.Object);

        var images = new List<CreateProjectImage>
        {
            new() { Id = _logoId, Type = ImageType.Logo },
            new() { Type = ImageType.Banner, Image = null },
            new() { Id = _cardId, Type = ImageType.Card }
        };

        var syncResult = await service.SyncProjectImagesAsync(_projectId, images);
        Assert.True(syncResult.IsSuccess, syncResult.Error?.Message);

        await using var verifyContext = new Database(new DbContextOptionsBuilder<Database>()
            .UseNpgsql("Host=localhost;Port=5433;Database=testdb;Username=testuser;Password=testpassword")
            .Options);

        var persistedImages = await verifyContext.ProjectImage.Where(i => i.ProjectId == _projectId).ToListAsync();

        var card = persistedImages.SingleOrDefault(i => i.Type == ImageType.Card);
        var banner = persistedImages.SingleOrDefault(i => i.Type == ImageType.Banner);
        var logo = persistedImages.SingleOrDefault(i => i.Type == ImageType.Logo);

        Assert.NotNull(card);
        Assert.Equal("originalCard.png", card.Image);
        Assert.NotNull(banner);
        Assert.Equal("newBanner.png", banner.Image);
        Assert.NotNull(logo);
        Assert.Equal("originalLogo.png", logo.Image);
    }
}
