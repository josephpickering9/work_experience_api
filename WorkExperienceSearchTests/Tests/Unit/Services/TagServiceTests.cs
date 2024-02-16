using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class TagServiceTests : BaseServiceTests, IAsyncLifetime
{
    private readonly TagService _tagService;

    public TagServiceTests()
    {
        _tagService = new TagService(Context);
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
    public async Task GetTagsAsync_NoSearch_ReturnsAllTags()
    {
        // Act
        var result = await _tagService.GetTagsAsync(null);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetTagAsync_ValidId_ReturnsTag()
    {
        // Arrange
        var tagId = 1;

        // Act
        var result = await _tagService.GetTagAsync(tagId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);
    }

    [Fact]
    public async Task GetTagAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tagId = 99;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _tagService.GetTagAsync(tagId));
    }

    [Fact]
    public async Task CreateTagAsync_NewTag_ReturnsTag()
    {
        // Arrange
        var createTag = new CreateTag
        {
            Title = "Vue",
            Type = TagType.Frontend,
            Icon = "",
            CustomColour = null
        };

        // Act
        var result = await _tagService.CreateTagAsync(createTag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal(createTag.Title, result.Title);
    }

    [Fact]
    public async Task CreateTagAsync_ExistingTag_ThrowsConflictException()
    {
        // Arrange
        var createTag = new CreateTag
        {
            Title = "C#",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _tagService.CreateTagAsync(createTag));
    }

    [Fact]
    public async Task UpdateTagAsync_ValidId_ReturnsUpdatedTag()
    {
        // Arrange
        var tagId = 1;
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act
        var result = await _tagService.UpdateTagAsync(tagId, updateTag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);
        Assert.Equal(updateTag.Title, result.Title);
    }

    [Fact]
    public async Task UpdateTagAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tagId = 99;
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _tagService.UpdateTagAsync(tagId, updateTag));
    }

    [Fact]
    public async Task DeleteTagAsync_ValidId_DeletesTag()
    {
        // Arrange
        var tagId = 1;

        // Act
        var result = await _tagService.DeleteTagAsync(tagId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);

        var tagInDb = await Context.Tag.FindAsync(tagId);
        Assert.Null(tagInDb);
    }

    [Fact]
    public async Task DeleteTagAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var tagId = 99;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _tagService.DeleteTagAsync(tagId));
    }

    private async Task SeedDatabase()
    {
        if (!Context.Tag.Any())
        {
            Context.Tag.AddRange(GetTestTags());
            await Context.SaveChangesAsync();
        }
    }

    private static List<Tag> GetTestTags()
    {
        var cSharpTag = CreateTag(1, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(2, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(3, "Xamarin Forms", TagType.Frontend);

        return [cSharpTag, aspNetCoreTag, xamarinFormsTag];
    }
}