using System.Net;
using System.Net.Http.Headers;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Work_Experience_Search;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Tests;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

public class ProjectControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetProjects_ReturnsProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            await CreateProjectAsync(1),
            await CreateProjectAsync(2),
            await CreateProjectAsync(3)
        };
        
        // Act
        var httpResponse = await Client.GetAsync("/project");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = GetJsonContent<List<Project>>(stringResponse);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(projects.Count, response.Count);
    }

    [Fact]
    public async Task GetProject_ExistingId_ReturnsProject()
    {
        // Arrange
        const int testProjectId = 10;
        var tags = new List<string> { "Tag1", "Tag2" };
        var expectedProject = await CreateProjectAsync(testProjectId, tags: tags);

        // Act
        var httpResponse = await Client.GetAsync($"/project/{testProjectId}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(expectedProject.Id, actualProject.Id);
        Assert.Equal(expectedProject.Title, actualProject.Title);
        Assert.Equal(expectedProject.Description, actualProject.Description);
        Assert.Equal(expectedProject.Company, actualProject.Company);
        Assert.Equal(expectedProject.Website, actualProject.Website);
        Assert.Equal(expectedProject.Tags.Count, actualProject.Tags.Count);
    }

    [Fact]
    public async Task GetProject_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingProjectId = 999;

        // Act
        var httpResponse = await Client.GetAsync($"/project/{nonExistingProjectId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetProject_ExistingSlug_ReturnsProject()
    {
        // Arrange
        const int testProjectId = 11;
        var expectedProject = await CreateProjectAsync(testProjectId);

        // Act
        var httpResponse = await Client.GetAsync($"/project/{expectedProject.Slug}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(expectedProject.Id, actualProject.Id);
        Assert.Equal(expectedProject.Title, actualProject.Title);
        Assert.Equal(expectedProject.Description, actualProject.Description);
        Assert.Equal(expectedProject.Company, actualProject.Company);
        Assert.Equal(expectedProject.Website, actualProject.Website);
        Assert.Equal(expectedProject.Tags.Count, actualProject.Tags.Count);
    }

    [Fact]
    public async Task GetProject_NonExistingSlug_ReturnsNotFound()
    {
        // Arrange
        const string nonExistingProjectSlug = "non-existing-slug";

        // Act
        var httpResponse = await Client.GetAsync($"/project/{nonExistingProjectSlug}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetRelatedProjects_ExistingId_ReturnsProjects()
    {
        // Arrange
        var tags = new List<string> { "Tag1", "Tag2" };
        var expectedProject = await CreateProjectAsync(1, tags: tags);
        var relatedProject = await CreateProjectAsync(2, tags: tags);

        // Act
        var httpResponse = await Client.GetAsync($"/project/{expectedProject.Id}/related");

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProjects = GetJsonContent<List<Project>>(stringResponse);

        Assert.NotNull(actualProjects);
        Assert.NotEmpty(actualProjects);
        Assert.Contains(relatedProject.Id, actualProjects.Select(p => p.Id));
    }

    [Fact]
    public async Task PostProject_CreatesNewProject()
    {
        // Arrange
        var newProject = new CreateProject
        {
            Title = "New Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"],
            Images = []
        };

        var content = GetMultipartFormDataContent(newProject);

        // Act
        var httpResponse = await AuthenticatedClient.PostAsync("/project", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(newProject.Title, actualProject.Title);
        Assert.Equal(newProject.Description, actualProject.Description);
        Assert.Equal(newProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(newProject.CompanyId, actualProject.CompanyId);
        Assert.Equal(newProject.Year, actualProject.Year);
        Assert.Equal(newProject.Website, actualProject.Website);
        Assert.NotNull(actualProject.Tags);
        Assert.Equal(newProject.Tags.Count, actualProject.Tags.Count);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectInDb = await context.Project.FindAsync(actualProject.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(newProject.Title, projectInDb.Title);
        Assert.Equal(newProject.Description, projectInDb.Description);
        Assert.Equal(newProject.ShortDescription, projectInDb.ShortDescription);
        Assert.Equal(newProject.CompanyId, projectInDb.CompanyId);
        Assert.Equal(newProject.Year, projectInDb.Year);
        Assert.Equal(newProject.Website, projectInDb.Website);

        if (projectInDb != null)
        {
            projectInDb.Tags =
                await context.Tag.Where(t => t.Projects.Any(p => p.Id == projectInDb.Id)).ToListAsync();
            Assert.NotNull(projectInDb.Tags);
            Assert.Equal(newProject.Tags.Count, projectInDb.Tags.Count);
        }
    }

    [Fact]
    public async Task PostProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var newProject = new CreateProject
        {
            Title = "New Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"]
        };

        var content = GetMultipartFormDataContent(newProject);

        // Act
        var httpResponse = await Client.PostAsync("/project", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostProject_WithDuplicateTitle_ReturnsConflict()
    {
        // Arrange
        var duplicateProject = new CreateProject
        {
            Title = "Duplicate Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"]
        };

        var content = GetMultipartFormDataContent(duplicateProject);

        // Act - First attempt (should succeed)
        var firstResponse = await AuthenticatedClient.PostAsync("/project", content);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Second attempt (should fail)
        var secondResponse = await AuthenticatedClient.PostAsync("/project", content);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var stringResponse = await secondResponse.Content.ReadAsStringAsync();

        Assert.Contains("A project with the same title already exists", stringResponse);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectCount = await context.Project.CountAsync(p => p.Title == duplicateProject.Title);
        Assert.Equal(1, projectCount);
    }

    [Fact]
    public async Task PutProject_ExistingId_UpdatesProject()
    {
        // Arrange
        const int testProjectId = 13;
        var existingProject = await CreateProjectAsync(testProjectId);

        var updateProject = new CreateProject
        {
            Title = "Updated Project",
            ShortDescription = "Updated short description",
            Description = "Updated long description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://updated-example.com",
            Tags = ["UpdatedTag1", "UpdatedTag2"]
        };

        var content = GetMultipartFormDataContent(updateProject);

        // Act
        var httpResponse = await AuthenticatedClient.PutAsync($"/project/{existingProject.Id}", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(updateProject.Title, actualProject.Title);
        Assert.Equal(updateProject.Description, actualProject.Description);
        Assert.Equal(updateProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(updateProject.CompanyId, actualProject.CompanyId);
        Assert.Equal(updateProject.Year, actualProject.Year);
        Assert.Equal(updateProject.Website, actualProject.Website);
        Assert.NotNull(actualProject.Tags);
        Assert.Equal(updateProject.Tags.Count, actualProject.Tags.Count);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectInDb = await context.Project.FindAsync(existingProject.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(updateProject.Title, projectInDb.Title);
        Assert.Equal(updateProject.Description, projectInDb.Description);
        Assert.Equal(updateProject.ShortDescription, projectInDb.ShortDescription);
        Assert.Equal(updateProject.CompanyId, projectInDb.CompanyId);
        Assert.Equal(updateProject.Year, projectInDb.Year);
        Assert.Equal(updateProject.Website, projectInDb.Website);

        if (projectInDb != null)
        {
            projectInDb.Tags =
                await context.Tag.Where(t => t.Projects.Any(p => p.Id == projectInDb.Id)).ToListAsync();
            Assert.NotNull(projectInDb.Tags);
            Assert.Equal(updateProject.Tags.Count, projectInDb.Tags.Count);
        }
    }

    [Fact]
    public async Task PutProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        const int testProjectId = 14;
        var existingProject = await CreateProjectAsync(testProjectId);

        // Act
        var updateProject = new CreateProject
        {
            Title = "Updated Project",
            ShortDescription = "Updated short description",
            Description = "Updated long description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://updated-example.com",
            Tags = ["UpdatedTag1", "UpdatedTag2"]
        };

        var content = GetMultipartFormDataContent(updateProject);

        // Act
        var httpResponse = await Client.PutAsync($"/project/{existingProject.Id}", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_ExistingId_DeletesProject()
    {
        // Arrange
        const int testProjectId = 15;
        var existingProject = await CreateProjectAsync(testProjectId);

        // Act
        var httpResponse = await AuthenticatedClient.DeleteAsync($"/project/{existingProject.Id}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectInDb = await context.Project.FindAsync(existingProject.Id);
        Assert.Null(projectInDb);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        const int testProjectId = 16;
        var existingProject = await CreateProjectAsync(testProjectId);

        // Act
        var httpResponse = await Client.DeleteAsync($"/project/{existingProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }
}