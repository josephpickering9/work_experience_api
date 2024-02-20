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

public class ProjectServiceTests : BaseServiceTests, IAsyncLifetime
{
    private readonly Mock<ITagService> _mockTagService;
    private readonly ProjectService _projectService;

    public ProjectServiceTests()
    {
        var mockFileService = new Mock<IFileService>();
        var mockProjectImageService = new Mock<IProjectImageService>();
        _mockTagService = new Mock<ITagService>();
        _projectService = new ProjectService(Context, mockProjectImageService.Object, _mockTagService.Object);

        mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(() => new Success<string>("testPath"));
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
    public async Task GetProjectsAsync_NoSearchTerm_ReturnsAllProjects()
    {
        // Arrange is done in the constructor

        // Act
        var result = (await _projectService.GetProjectsAsync(null)).ExpectSuccess().ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Assuming GetTestProjects() returns 3 projects
    }

    [Fact]
    public async Task GetProjectsAsync_WithSearchTerm_ReturnsMatchingProjects()
    {
        // Arrange is done in the constructor

        // Act
        var result = (await _projectService.GetProjectsAsync("ViSIT")).ExpectSuccess().ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Visit Northumberland", result.First().Title);
    }

    [Fact]
    public async Task GetProjectAsync_ValidId_ReturnsProject()
    {
        // Arrange
        const int testProjectId = 1; // Assuming this ID exists in GetTestProjects()

        // Act
        var result = (await _projectService.GetProjectAsync(testProjectId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testProjectId, result.Id);
    }

    [Fact]
    public async Task GetProjectBySlugAsync_ValidSlug_ReturnsProject()
    {
        // Arrange
        const string validSlug = "visit-northumberland";
        await SaveProject(CreateProject(5, "Visit Northumberland", "Visit Northumberland Description",
            "Visit Northumberland Short Description", 1, 2021,
            "https://visitnorthumberland.com", new List<Tag>()));

        // Act
        var result = (await _projectService.GetProjectBySlugAsync(validSlug)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validSlug, result.Slug);
    }

    [Fact]
    public async Task GetProjectBySlugAsync_InvalidSlug_ThrowsNotFoundFailure()
    {
        // Arrange
        const string invalidSlug = "non-existent-slug";

        // Act
        var result = (await _projectService.GetProjectBySlugAsync(invalidSlug)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Project not found.", result.Message);
    }

    [Fact]
    public async Task GetRelatedProjectsAsync_WithCommonTags_ReturnsRelatedProjects()
    {
        // Arrange
        const int projectIdWithTags = 1;

        // Act
        var result = (await _projectService.GetRelatedProjectsAsync(projectIdWithTags)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Any());
    }

    [Fact]
    public async Task GetRelatedProjectsAsync_GetsProjectsWithMostRelatedTags()
    {
        // Arrange
        await ClearDatabase();

        var tag1 = CreateTag(1, "Tag 1", TagType.Default);
        var tag2 = CreateTag(2, "Tag 2", TagType.Backend);
        var tag3 = CreateTag(3, "Tag 3", TagType.Frontend);
        var tag4 = CreateTag(4, "Tag 4", TagType.DevOps);
        var tag5 = CreateTag(5, "Tag 5", TagType.Data);
        var tag6 = CreateTag(6, "Tag 6", TagType.Mobile);

        var mainProject = await SaveProject(CreateProject(1, "Project 1", tags: [tag1, tag2, tag3, tag4, tag5, tag6]));

        var relatedProject1 = await SaveProject(CreateProject(2, "Project 2", tags: [tag1, tag2]));
        var relatedProject2 = await SaveProject(CreateProject(3, "Project 3", tags: [tag1, tag2, tag3]));
        var relatedProject3 = await SaveProject(CreateProject(4, "Project 4", tags: [tag1, tag2, tag3, tag4]));
        var relatedProject4 = await SaveProject(CreateProject(5, "Project 5", tags: [tag1, tag2, tag3, tag4, tag5]));
        var relatedProject5 =
            await SaveProject(CreateProject(6, "Project 6", tags: [tag1, tag2, tag3, tag4, tag5, tag6]));

        // Act
        var relatedProjects = (await _projectService.GetRelatedProjectsAsync(mainProject.Id)).ExpectSuccess().ToList();

        // Assert
        Assert.NotNull(relatedProjects);
        Assert.Equal(3, relatedProjects.Count);
        Assert.DoesNotContain(relatedProjects, p => p.Id == relatedProject1.Id);
        Assert.DoesNotContain(relatedProjects, p => p.Id == relatedProject2.Id);
        Assert.Contains(relatedProjects, p => p.Id == relatedProject3.Id);
        Assert.Contains(relatedProjects, p => p.Id == relatedProject4.Id);
        Assert.Contains(relatedProjects, p => p.Id == relatedProject5.Id);
    }

    [Fact]
    public async Task CreateProjectAsync_NewProject_ReturnsProject()
    {
        // Arrange
        var newProject = new CreateProject
        {
            Title = "Test Project",
            Description = "Test Description",
            ShortDescription = "Test Short Description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://example.com",
            Tags = ["Test Tag"]
        };

        _mockTagService.Setup(ts => ts.SyncTagsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync((List<string> tags) =>
                new Success<List<Tag>>(tags.Select(t => CreateTag(4, t, TagType.Default)).ToList()));

        // Act
        var result = (await _projectService.CreateProjectAsync(newProject)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newProject.Title, result.Title);
        Assert.Equal(newProject.Description, result.Description);
        Assert.Equal(newProject.ShortDescription, result.ShortDescription);
        Assert.Equal(newProject.CompanyId, result.CompanyId);
        Assert.Equal(newProject.Year, result.Year);
        Assert.Equal(newProject.Website, result.Website);

        var projectInDb = await Context.Project.FindAsync(result.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(newProject.Title, projectInDb.Title);
        Assert.NotNull(projectInDb.Tags);
        Assert.Equal(newProject.Tags.Count, projectInDb.Tags.Count);
        Assert.Contains(projectInDb.Tags, t => t.Title == "Test Tag");
    }

    [Fact]
    public async Task UpdateProjectAsync_ExistingProject_UpdatesProject()
    {
        // Arrange
        var tag = CreateTag(4, "Updated Tag", TagType.Backend);
        var existingProject = await SaveProject(CreateProject(4, "Test Update Project", "Test Description",
            "Test Short Description", 1, 2021,
            "https://example.com", [tag]));

        var updateData = new CreateProject
        {
            Title = "Updated Project",
            Description = "Updated Description",
            ShortDescription = "Updated Short Description",
            CompanyId = 1,
            Year = 2021,
            Website = "https://updated.com",
            Tags = ["Updated Tag"]
        };

        _mockTagService.Setup(ts => ts.SyncTagsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(() => new Success<List<Tag>>([tag]));

        // Act
        var result = (await _projectService.UpdateProjectAsync(existingProject.Id, updateData)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateData.Title, result.Title);
        Assert.Equal(updateData.Description, result.Description);
        Assert.Equal(updateData.ShortDescription, result.ShortDescription);
        Assert.Equal(updateData.CompanyId, result.CompanyId);
        Assert.Equal(updateData.Year, result.Year);
        Assert.Equal(updateData.Website, result.Website);

        var projectInDb = await Context.Project.FindAsync(existingProject.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(updateData.Title, projectInDb.Title);
        Assert.Equal(updateData.Description, projectInDb.Description);
        Assert.Equal(updateData.ShortDescription, projectInDb.ShortDescription);
        Assert.Equal(updateData.CompanyId, projectInDb.CompanyId);
        Assert.Equal(updateData.Year, projectInDb.Year);
        Assert.Equal(updateData.Website, projectInDb.Website);
        Assert.NotNull(projectInDb.Tags);
        Assert.Equal(updateData.Tags.Count, projectInDb.Tags.Count);
        Assert.Contains(projectInDb.Tags, t => t.Title == "Updated Tag");
    }

    [Fact]
    public async Task DeleteProjectAsync_ExistingProject_DeletesProject()
    {
        // Arrange
        var existingProject = await SaveProject(CreateProject(6, "Test Delete Project"));

        // Act
        var result = (await _projectService.DeleteProjectAsync(existingProject.Id)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProject.Id, result.Id);

        var projectInDb = await Context.Project.FindAsync(existingProject.Id);
        Assert.Null(projectInDb);
    }

    private async Task SeedDatabase()
    {
        if (!Context.Project.Any())
        {
            Context.Project.AddRange(GetTestProjects());
            await Context.SaveChangesAsync();
        }
    }

    private async Task<Project> SaveProject(Project project)
    {
        await Context.Project.AddAsync(project);
        await Context.SaveChangesAsync();
        return project;
    }

    private static IEnumerable<Tag> GetTestTags()
    {
        return
        [
            CreateTag(1, "C#", TagType.Backend),
            CreateTag(2, "ASP.NET Core", TagType.Backend),
            CreateTag(3, "Xamarin Forms", TagType.Frontend)
        ];
    }

    private static Company GetTestCompany()
    {
        var company = CreateCompany(1, "Drummond Central", "A marketing agency based in Newcastle upon Tyne.",
            "https://drummondcentral.co.uk/wp-content/uploads/2019/10/DC-Logo-White.png",
            "https://drummondcentral.co.uk/");

        return company;
    }

    private static IEnumerable<Project> GetTestProjects()
    {
        var cSharpTag = CreateTag(1, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(2, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(3, "Xamarin Forms", TagType.Frontend);

        var company = CreateCompany(1, "Drummond Central", "A marketing agency based in Newcastle upon Tyne.",
            "https://drummondcentral.co.uk/wp-content/uploads/2019/10/DC-Logo-White.png",
            "https://drummondcentral.co.uk/");

        return
        [
            CreateProject(1, "Visit Northumberland",
                "A website for Visit Northumberland using C# and ASP.NET Core MVC.",
                "A website for Visit Northumberland", company.Id,
                2021, "https://visitnorthumberland.com/", [cSharpTag, aspNetCoreTag]),

            CreateProject(2, "BeatCovidNE", "A website for BeatCovidNE using C# and ASP.NET Core MVC.",
                "A website for BeatCovidNE", company.Id, 2021,
                "https://beatcovidne.co.uk/", [cSharpTag, aspNetCoreTag]),

            CreateProject(3, "taxigoat",
                "A website & mobile application for taxigoat using Xamarin Forms and ASP.NET Core API.",
                "A website for taxigoat", company.Id, 2021,
                "https://taxigoat.co.uk/", [xamarinFormsTag])
        ];
    }
}