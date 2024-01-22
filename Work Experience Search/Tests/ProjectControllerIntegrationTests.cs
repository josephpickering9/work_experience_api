using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Xunit;

namespace Work_Experience_Search.Tests;

public class ProjectControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProjectControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProjects_ReturnsProjects()
    {
        // Arrange
        HttpResponseMessage httpResponse = await _client.GetAsync("/project");

        // Act
        httpResponse.EnsureSuccessStatusCode();
        string stringResponse = await httpResponse.Content.ReadAsStringAsync();
        IEnumerable<Project>? projects = JsonConvert.DeserializeObject<IEnumerable<Project>>(stringResponse);

        // Assert
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
    }
    
    [Fact]
    public async Task GetProject_ExistingId_ReturnsProject()
    {
        // Arrange
        int testProjectId = 10; 
        Project expectedProject = await CreateProjectAsync(testProjectId);

        // Act
        HttpResponseMessage httpResponse = await _client.GetAsync($"/project/id?id={testProjectId}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        string stringResponse = await httpResponse.Content.ReadAsStringAsync();
        Project? actualProject = JsonConvert.DeserializeObject<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(expectedProject.Id, actualProject.Id);
        Assert.Equal(expectedProject.Title, actualProject.Title);
        Assert.Equal(expectedProject.Description, actualProject.Description);
        Assert.Equal(expectedProject.Company, actualProject.Company);
        Assert.Equal(expectedProject.Website, actualProject.Website);
        Assert.Equal(expectedProject.Image, actualProject.Image);
        Assert.Equal(expectedProject.BackgroundImage, actualProject.BackgroundImage);
        Assert.Equal(expectedProject.Tags.Count, actualProject.Tags.Count);
    }

    [Fact]
    public async Task GetProject_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        int nonExistingProjectId = 999;

        // Act
        HttpResponseMessage httpResponse = await _client.GetAsync($"/project/id?id={nonExistingProjectId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PostProject_CreatesNewProject()
    {
        // Arrange
        CreateProject newProject = new CreateProject
        {
            Title = "New Project",
            ShortDescription = "A short description",
            Description = "A long description",
            Company = "Test Company",
            Image = null,
            BackgroundImage = null,
            Year = 2021,
            Website = "https://example.com",
            Tags = new List<string> { "Tag1", "Tag2" }
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(newProject), Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage httpResponse = await _client.PostAsync("/project", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        string stringResponse = await httpResponse.Content.ReadAsStringAsync();
        Project? actualProject = JsonConvert.DeserializeObject<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(newProject.Title, actualProject.Title);
        Assert.Equal(newProject.Description, actualProject.Description);
        Assert.Equal(newProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(newProject.Company, actualProject.Company);
        Assert.Equal(newProject.Year, actualProject.Year);
        Assert.Equal(newProject.Website, actualProject.Website);
        Assert.NotNull(actualProject.Tags);
        Assert.Equal(newProject.Tags.Count, actualProject.Tags.Count);

        using (IServiceScope? scope = _factory.Services.CreateScope())
        {
            Database? context = scope.ServiceProvider.GetRequiredService<Database>();
            Project? projectInDb = await context.Project.FindAsync(actualProject.Id);
            Assert.NotNull(projectInDb);
            Assert.Equal(newProject.Title, projectInDb.Title);
            Assert.Equal(newProject.Description, projectInDb.Description);
            Assert.Equal(newProject.ShortDescription, projectInDb.ShortDescription);
            Assert.Equal(newProject.Company, projectInDb.Company);
            Assert.Equal(newProject.Year, projectInDb.Year);
            Assert.Equal(newProject.Website, projectInDb.Website);
            
            if (projectInDb != null)
            {
                projectInDb.Tags = await context.Tag.Where(t => t.Projects.Any(p => p.Id == projectInDb.Id)).ToListAsync();
                Assert.NotNull(projectInDb.Tags);
                Assert.Equal(newProject.Tags.Count, projectInDb.Tags.Count);
            }
        }
    }
    
    [Fact]
    public async Task PostProject_WithDuplicateTitle_ReturnsConflict()
    {
        // Arrange
        CreateProject duplicateProject = new CreateProject
        {
            Title = "Duplicate Project",
            ShortDescription = "A short description",
            Description = "A long description",
            Company = "Test Company",
            Image = null,
            BackgroundImage = null,
            Year = 2021,
            Website = "https://example.com",
            Tags = new List<string> { "Tag1", "Tag2" }
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(duplicateProject), Encoding.UTF8, "application/json");

        // Act - First attempt (should succeed)
        HttpResponseMessage firstResponse = await _client.PostAsync("/project", content);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Second attempt (should fail)
        HttpResponseMessage secondResponse = await _client.PostAsync("/project", content);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        string stringResponse = await secondResponse.Content.ReadAsStringAsync();

        Assert.Contains("A project with the same title already exists", stringResponse);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            Database context = scope.ServiceProvider.GetRequiredService<Database>();
            int projectCount = await context.Project.CountAsync(p => p.Title == duplicateProject.Title);
            Assert.Equal(1, projectCount);
        }
    }
    
    [Fact]
    public async Task PutProject_ExistingId_UpdatesProject()
    {
        // Arrange
        int testProjectId = 12; 
        Project existingProject = await CreateProjectAsync(testProjectId);

        CreateProject updateProject = new CreateProject
        {
            Title = "Updated Project",
            ShortDescription = "Updated short description",
            Description = "Updated long description",
            Company = "Updated Test Company",
            Year = 2021,
            Website = "https://updated-example.com",
            Tags = new List<string> { "UpdatedTag1", "UpdatedTag2" }
        };

        StringContent content = new StringContent(JsonConvert.SerializeObject(updateProject), Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage httpResponse = await _client.PutAsync($"/project/{existingProject.Id}", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        string stringResponse = await httpResponse.Content.ReadAsStringAsync();
        Project? actualProject = JsonConvert.DeserializeObject<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(updateProject.Title, actualProject.Title);
        Assert.Equal(updateProject.Description, actualProject.Description);
        Assert.Equal(updateProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(updateProject.Company, actualProject.Company);
        Assert.Equal(updateProject.Year, actualProject.Year);
        Assert.Equal(updateProject.Website, actualProject.Website);
        Assert.NotNull(actualProject.Tags);
        Assert.Equal(updateProject.Tags.Count, actualProject.Tags.Count);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            Database context = scope.ServiceProvider.GetRequiredService<Database>();
            Project? projectInDb = await context.Project.FindAsync(existingProject.Id);
            Assert.NotNull(projectInDb);
            Assert.Equal(updateProject.Title, projectInDb.Title);
            Assert.Equal(updateProject.Description, projectInDb.Description);
            Assert.Equal(updateProject.ShortDescription, projectInDb.ShortDescription);
            Assert.Equal(updateProject.Company, projectInDb.Company);
            Assert.Equal(updateProject.Year, projectInDb.Year);
            Assert.Equal(updateProject.Website, projectInDb.Website);
            
            if (projectInDb != null)
            {
                projectInDb.Tags = await context.Tag.Where(t => t.Projects.Any(p => p.Id == projectInDb.Id)).ToListAsync();
                Assert.NotNull(projectInDb.Tags);
                Assert.Equal(updateProject.Tags.Count, projectInDb.Tags.Count);
            }
        }
    }
    
    [Fact]
    public async Task DeleteProject_ExistingId_DeletesProject()
    {
        // Arrange
        int testProjectId = 13; 
        Project existingProject = await CreateProjectAsync(testProjectId);

        // Act
        HttpResponseMessage httpResponse = await _client.DeleteAsync($"/project/id?id={existingProject.Id}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            Database context = scope.ServiceProvider.GetRequiredService<Database>();
            Project? projectInDb = await context.Project.FindAsync(existingProject.Id);
            Assert.Null(projectInDb);
        }
    }    
    
    private async Task<Project> CreateProjectAsync(
        int projectId,
        string title = "Test Project",
        string description = "Test Description",
        string shortDescription = "Test Short Description",
        string company = "Test Company",
        string? image = null,
        string? backgroundImage = null,
        int year = 2021,
        string website = "https://example.com",
        List<Tag>? tags = null
    ) {
        Project project = new Project
        {
            Id = projectId,
            Title = title,
            Description = description,
            ShortDescription = shortDescription,
            Company = company,
            Image = image,
            BackgroundImage = backgroundImage,
            Year = year,
            Website = website,
            Tags = tags ?? new List<Tag>()
        };

        using IServiceScope scope = _factory.Services.CreateScope();
        Database? context = scope.ServiceProvider.GetRequiredService<Database>();
        
        context.Project.Add(project);
        await context.SaveChangesAsync();
        return project;
    }
}
