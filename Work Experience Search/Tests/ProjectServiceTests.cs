using Microsoft.EntityFrameworkCore;
using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Xunit;

namespace Work_Experience_Search.Tests;

public class ProjectServiceTests
{
    private readonly Database _context;
    private readonly Mock<IFileService> _mockFileService;
    private readonly ProjectService _projectService;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseInMemoryDatabase("TestDatabase-" + new Guid())
            .Options;

        _context = new Database(options);
        _mockFileService = new Mock<IFileService>();
        _projectService = new ProjectService(_context, _mockFileService.Object);

        SeedDatabase();

        _mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((IFormFile? file) => file != null ? "testPath" : null);
    }

    [Fact]
    public async Task GetProjectsAsync_NoSearchTerm_ReturnsAllProjects()
    {
        // Arrange is done in the constructor

        // Act
        var result = await _projectService.GetProjectsAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, ((List<Project>)result).Count); // Assuming GetTestProjects() returns 3 projects
    }

    [Fact]
    public async Task GetProjectsAsync_WithSearchTerm_ReturnsMatchingProjects()
    {
        // Arrange is done in the constructor

        // Act
        var result = await _projectService.GetProjectsAsync("ViSIT");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Visit Northumberland", result.First().Title);
    }

    [Fact]
    public async Task GetProjectAsync_ValidId_ReturnsProject()
    {
        // Arrange
        var testProjectId = 1; // Assuming this ID exists in GetTestProjects()

        // Act
        var result = await _projectService.GetProjectAsync(testProjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testProjectId, result.Id);
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
            Company = "Test Company",
            Image = null,
            BackgroundImage = null,
            Year = 2021,
            Website = "https://example.com",
            Tags = new List<string> { "Test Tag" }
        };

        _mockFileService.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((IFormFile file) => file != null ? "testPath" : null);

        // Act
        var result = await _projectService.CreateProjectAsync(newProject);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newProject.Title, result.Title);
        Assert.Equal(newProject.Description, result.Description);
        Assert.Equal(newProject.ShortDescription, result.ShortDescription);
        Assert.Equal(newProject.Company, result.Company);
        Assert.Equal(newProject.Year, result.Year);
        Assert.Equal(newProject.Website, result.Website);

        var projectInDb = await _context.Project.FindAsync(result.Id);
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
        var existingProject = await SaveProject(CreateProject(5, "Test Update Project", "Test Description",
            "Test Short Description", "Test Company", new Guid().ToString(), new Guid().ToString(), 2021,
            "https://example.com", new List<Tag> { CreateTag(5, "Updated Tag", TagType.Backend) }));

        var updateData = new CreateProject
        {
            Title = "Updated Project",
            Description = "Updated Description",
            ShortDescription = "Updated Short Description",
            Company = "Updated Company",
            Image = null,
            BackgroundImage = null,
            Year = 2021,
            Website = "https://updated.com",
            Tags = new List<string> { "Updated Tag" }
        };

        // Act
        var result = await _projectService.UpdateProjectAsync(existingProject.Id, updateData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateData.Title, result.Title);
        Assert.Equal(updateData.Description, result.Description);
        Assert.Equal(updateData.ShortDescription, result.ShortDescription);
        Assert.Equal(updateData.Company, result.Company);
        Assert.Equal(updateData.Year, result.Year);
        Assert.Equal(updateData.Website, result.Website);

        // Ensure the project is updated in the database
        var projectInDb = await _context.Project.FindAsync(existingProject.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(updateData.Title, projectInDb.Title);
        Assert.Equal(updateData.Description, projectInDb.Description);
        Assert.Equal(updateData.ShortDescription, projectInDb.ShortDescription);
        Assert.Equal(updateData.Company, projectInDb.Company);
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
        var existingProject = await SaveProject(CreateProject(6, "Test Delete Project", "Test Description",
            "Test Short Description", "Test Company", new Guid().ToString(), new Guid().ToString(), 2021,
            "https://example.com", new List<Tag> { CreateTag(6, "Updated Tag", TagType.Backend) }));

        // Act
        var result = await _projectService.DeleteProjectAsync(existingProject.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProject.Id, result.Id);

        var projectInDb = await _context.Project.FindAsync(existingProject.Id);
        Assert.Null(projectInDb);
    }

    private void SeedDatabase()
    {
        if (!_context.Project.Any())
        {
            _context.Project.AddRange(GetTestProjects());
            _context.SaveChanges();
        }
    }

    private async Task<Project> SaveProject(Project project)
    {
        await _context.Project.AddAsync(project);
        _context.SaveChanges();
        return project;
    }

    private static List<Project> GetTestProjects()
    {
        var cSharpTag = CreateTag(1, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(2, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(3, "Xamarin Forms", TagType.Frontend);

        return new List<Project>
        {
            CreateProject(1, "Visit Northumberland",
                "A website for Visit Northumberland using C# and ASP.NET Core MVC.",
                "A website for Visit Northumberland", "Drummond Central", new Guid().ToString(), new Guid().ToString(),
                2021, "https://visitnorthumberland.com/", new List<Tag> { cSharpTag, aspNetCoreTag }),
            CreateProject(2, "BeatCovidNE", "A website for BeatCovidNE using C# and ASP.NET Core MVC.",
                "A website for BeatCovidNE", "Drummond Central", new Guid().ToString(), new Guid().ToString(), 2021,
                "https://beatcovidne.co.uk/", new List<Tag> { cSharpTag, aspNetCoreTag }),
            CreateProject(3, "taxigoat",
                "A website & mobile application for taxigoat using Xamarin Forms and ASP.NET Core API.",
                "A website for taxigoat", "Drummond Central", new Guid().ToString(), new Guid().ToString(), 2021,
                "https://taxigoat.co.uk/", new List<Tag> { xamarinFormsTag })
        };
    }

    private static Tag CreateTag(int id, string title, TagType type)
    {
        return new Tag
        {
            Id = id,
            Title = title,
            Type = type,
            Colour = "BLUE"
        };
    }

    private static Project CreateProject(int id, string title, string description, string shortDescription,
        string company, string image, string backgroundImage, int year, string website, List<Tag> tags)
    {
        return new Project
        {
            Id = id,
            Title = title,
            Description = description,
            ShortDescription = shortDescription,
            Company = company,
            Image = image,
            BackgroundImage = backgroundImage,
            Year = year,
            Website = website,
            Tags = tags
        };
    }
}
