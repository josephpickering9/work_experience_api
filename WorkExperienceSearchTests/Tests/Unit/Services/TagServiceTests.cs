using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class TagServiceTests : BaseServiceTests
{
    private readonly TagService _tagService;

    public TagServiceTests()
    {
        _tagService = new TagService(Context);
    }

    [Fact]
    public async Task GetTagsAsync_NoSearch_ReturnsAllTags()
    {
        // Act
        var result = (await _tagService.GetTagsAsync(null)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetTagAsync_ValidId_ReturnsTag()
    {
        // Arrange
        const int tagId = 1;

        // Act
        var result = (await _tagService.GetTagAsync(tagId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);
    }

    [Fact]
    public async Task GetTagAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int tagId = 99;

        // Act 
        var result = (await _tagService.GetTagAsync(tagId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Tag not found.", result.Message);
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
        var result = (await _tagService.CreateTagAsync(createTag)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal(createTag.Title, result.Title);
    }

    [Fact]
    public async Task CreateTagAsync_ExistingTag_ThrowsConflictFailure()
    {
        // Arrange
        var createTag = new CreateTag
        {
            Title = "C#",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act 
        var result = (await _tagService.CreateTagAsync(createTag)).ExpectFailure();

        // Assert
        Assert.IsType<ConflictException>(result);
        Assert.Equal("A tag with the same title already exists.", result.Message);
    }

    [Fact]
    public async Task UpdateTagAsync_ValidId_ReturnsUpdatedTag()
    {
        // Arrange
        const int tagId = 1;
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act
        var result = (await _tagService.UpdateTagAsync(tagId, updateTag)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);
        Assert.Equal(updateTag.Title, result.Title);
    }

    [Fact]
    public async Task UpdateTagAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int tagId = 99;
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        // Act 
        var result = (await _tagService.UpdateTagAsync(tagId, updateTag)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Tag not found.", result.Message);
    }

    [Fact]
    public async Task DeleteTagAsync_ValidId_DeletesTag()
    {
        // Arrange
        const int tagId = 1;

        // Act
        var result = (await _tagService.DeleteTagAsync(tagId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagId, result.Id);

        var tagInDb = await Context.Tag.FindAsync(tagId);
        Assert.Null(tagInDb);
    }

    [Fact]
    public async Task DeleteTagAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int tagId = 99;

        // Act 
        var result = (await _tagService.DeleteTagAsync(tagId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Tag not found.", result.Message);
    }

    private async Task SeedDatabase()
    {
        if (!Context.Tag.Any())
        {
            Context.Tag.AddRange(GetTestTags());
            await Context.SaveChangesAsync();
        }
    }

    private static IEnumerable<Tag> GetTestTags()
    {
        var cSharpTag = CreateTag(1, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(2, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(3, "Xamarin Forms", TagType.Frontend);

        return [cSharpTag, aspNetCoreTag, xamarinFormsTag];
    }
}