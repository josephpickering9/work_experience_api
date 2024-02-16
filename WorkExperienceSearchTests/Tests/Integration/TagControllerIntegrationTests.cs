using System.Net;
using Newtonsoft.Json;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Tests;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

public class TagControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetTags_ReturnsTags()
    {
        // Arrange
        var tags = new List<Tag>
        {
            await CreateTagAsync(1, "Tag 1"),
            await CreateTagAsync(2, "Tag 2"),
            await CreateTagAsync(3, "Tag 3")
        };

        // Act
        var httpResponse = await Client.GetAsync("/tag");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = GetJsonContent<List<Tag>>(stringResponse);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(tags.Count, response.Count);
    }
    
    [Fact]
    public async Task GetTag_ExistingId_ReturnsTag()
    {
        // Arrange
        const int testTagId = 1;
        await CreateTagAsync(testTagId);
        
        // Act
        var httpResponse = await Client.GetAsync($"/tag/{testTagId}");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);
        
        // Assert
        Assert.NotNull(tag);
        Assert.Equal(testTagId, tag.Id);
    }
    
    [Fact]
    public async Task GetTag_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingTagId = 999;
        
        // Act
        var httpResponse = await Client.GetAsync($"/tag/{nonExistingTagId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PostTag_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var createTag = new CreateTag
        {
            Title = "New Tag",
            Type = TagType.Frontend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await Client.PostAsync("/tag", CreateJsonContent(createTag));
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PostTag_ValidTag_ReturnsTag()
    {
        // Arrange
        var createTag = new CreateTag
        {
            Title = "New Tag",
            Type = TagType.Frontend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await AuthenticatedClient.PostAsync("/tag", CreateJsonContent(createTag));
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);
        
        // Assert
        Assert.NotNull(tag);
        Assert.Equal(1, tag.Id);
        Assert.Equal(createTag.Title, tag.Title);
    }
    
    [Fact]
    public async Task PostTag_ExistingTag_ReturnsConflict()
    {
        // Arrange
        await CreateTagAsync(1, title: "Conflict Tag");
        var createTag = new CreateTag
        {
            Title = "Conflict Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await AuthenticatedClient.PostAsync("/tag", CreateJsonContent(createTag));
        
        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PutTag_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await Client.PutAsync("/tag/1", CreateJsonContent(updateTag));
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PutTag_ValidId_ReturnsUpdatedTag()
    {
        // Arrange
        const int tagId = 1;
        await CreateTagAsync(tagId);
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await AuthenticatedClient.PutAsync($"/tag/{tagId}", CreateJsonContent(updateTag));
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);
        
        // Assert
        Assert.NotNull(tag);
        Assert.Equal(tagId, tag.Id);
        Assert.Equal(updateTag.Title, tag.Title);
    }
    
    [Fact]
    public async Task PutTag_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingTagId = 999;
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };
        
        // Act
        var httpResponse = await AuthenticatedClient.PutAsync($"/tag/{nonExistingTagId}", CreateJsonContent(updateTag));
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteTag_Unauthorized_ReturnsUnauthorized()
    {
        // Act
        var httpResponse = await Client.DeleteAsync("/tag/1");
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteTag_ValidId_DeletesTag()
    {
        // Arrange
        const int tagId = 1;
        await CreateTagAsync(tagId);
        
        // Act
        var httpResponse = await AuthenticatedClient.DeleteAsync($"/tag/{tagId}");
        httpResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteTag_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingTagId = 999;
        
        // Act
        var httpResponse = await AuthenticatedClient.DeleteAsync($"/tag/{nonExistingTagId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
}