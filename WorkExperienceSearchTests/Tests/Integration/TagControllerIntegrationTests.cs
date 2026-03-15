using System.Net;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Tests;
using Work_Experience_Search.Types;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

[Collection("Sequential")]
public class TagControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetTags_ReturnsTags()
    {
        var tags = new List<Tag>
        {
            await CreateTagAsync(TagId.New(), "Tag 1"),
            await CreateTagAsync(TagId.New(), "Tag 2"),
            await CreateTagAsync(TagId.New(), "Tag 3")
        };

        var httpResponse = await Client.GetAsync("/tag");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = GetJsonContent<List<Tag>>(stringResponse);

        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(tags.Count, response.Count);
    }

    [Fact]
    public async Task GetTag_ExistingId_ReturnsTag()
    {
        var testTagId = TagId.New();
        await CreateTagAsync(testTagId);

        var httpResponse = await Client.GetAsync($"/tag/{testTagId}");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);

        Assert.NotNull(tag);
        Assert.Equal(testTagId, tag.Id);
    }

    [Fact]
    public async Task GetTag_NonExistingId_ReturnsNotFound()
    {
        var nonExistingTagId = TagId.New();

        var httpResponse = await Client.GetAsync($"/tag/{nonExistingTagId}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostTag_Unauthorized_ReturnsUnauthorized()
    {
        var createTag = new CreateTag
        {
            Title = "New Tag",
            Type = TagType.Frontend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await Client.PostAsync("/tag", CreateJsonContent(createTag));

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostTag_ValidTag_ReturnsTag()
    {
        var createTag = new CreateTag
        {
            Title = "New Tag",
            Type = TagType.Frontend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await AuthenticatedClient.PostAsync("/tag", CreateJsonContent(createTag));
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);

        Assert.NotNull(tag);
        Assert.Equal(createTag.Title, tag.Title);
        Assert.Equal(createTag.Type, tag.Type);
        Assert.Equal(createTag.Icon, tag.Icon);
        Assert.Equal(createTag.CustomColour, tag.CustomColour);
    }

    [Fact]
    public async Task PostTag_ExistingTag_ReturnsConflict()
    {
        await CreateTagAsync(TagId.New(), "Conflict Tag");
        var createTag = new CreateTag
        {
            Title = "Conflict Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await AuthenticatedClient.PostAsync("/tag", CreateJsonContent(createTag));

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PutTag_Unauthorized_ReturnsUnauthorized()
    {
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await Client.PutAsync($"/tag/{TagId.New()}", CreateJsonContent(updateTag));

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PutTag_ValidId_ReturnsUpdatedTag()
    {
        var tagId = TagId.New();
        await CreateTagAsync(tagId);
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await AuthenticatedClient.PutAsync($"/tag/{tagId}", CreateJsonContent(updateTag));
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var tag = GetJsonContent<Tag>(stringResponse);

        Assert.NotNull(tag);
        Assert.Equal(tagId, tag.Id);
        Assert.Equal(updateTag.Title, tag.Title);
    }

    [Fact]
    public async Task PutTag_NonExistingId_ReturnsNotFound()
    {
        var nonExistingTagId = TagId.New();
        var updateTag = new CreateTag
        {
            Title = "Updated Tag",
            Type = TagType.Backend,
            Icon = "",
            CustomColour = null
        };

        var httpResponse = await AuthenticatedClient.PutAsync($"/tag/{nonExistingTagId}", CreateJsonContent(updateTag));

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_Unauthorized_ReturnsUnauthorized()
    {
        var httpResponse = await Client.DeleteAsync($"/tag/{TagId.New()}");

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_ValidId_DeletesTag()
    {
        var tagId = TagId.New();
        await CreateTagAsync(tagId);

        var httpResponse = await AuthenticatedClient.DeleteAsync($"/tag/{tagId}");
        httpResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DeleteTag_NonExistingId_ReturnsNotFound()
    {
        var nonExistingTagId = TagId.New();

        var httpResponse = await AuthenticatedClient.DeleteAsync($"/tag/{nonExistingTagId}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
}
