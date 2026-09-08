using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Tests;
using Work_Experience_Search.Types;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

[Collection("Sequential")]
public class ProjectControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetProjects_ReturnsProjects()
    {
        var projects = new List<Project>
        {
            await CreateProjectAsync(ProjectId.New()),
            await CreateProjectAsync(ProjectId.New()),
            await CreateProjectAsync(ProjectId.New())
        };

        var httpResponse = await Client.GetAsync("/project");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = GetJsonContent<List<Project>>(stringResponse);

        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(projects.Count, response.Count);
    }

    [Fact]
    public async Task GetProject_ExistingId_ReturnsProject()
    {
        var testProjectId = ProjectId.New();
        var tags = new List<string> { "Tag1", "Tag2" };
        var expectedProject = await CreateProjectAsync(testProjectId, tags: tags);

        var httpResponse = await Client.GetAsync($"/project/{testProjectId}");

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
        var nonExistingProjectId = ProjectId.New();

        var httpResponse = await Client.GetAsync($"/project/{nonExistingProjectId}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetProject_ExistingSlug_ReturnsProject()
    {
        var testProjectId = ProjectId.New();
        var expectedProject = await CreateProjectAsync(testProjectId);

        var httpResponse = await Client.GetAsync($"/project/{expectedProject.Slug}");

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
        const string nonExistingProjectSlug = "non-existing-slug";

        var httpResponse = await Client.GetAsync($"/project/{nonExistingProjectSlug}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task GetRelatedProjects_ExistingId_ReturnsProjects()
    {
        var tags = new List<string> { "Tag1", "Tag2" };
        var expectedProject = await CreateProjectAsync(ProjectId.New(), tags: tags);
        var relatedProject = await CreateProjectAsync(ProjectId.New(), tags: tags);

        var httpResponse = await Client.GetAsync($"/project/{expectedProject.Id}/related");

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
        var newProject = new CreateProject
        {
            Title = "New Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = null,
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"],
            Images = [],
            Repositories = [],
        };

        var content = GetMultipartFormDataContent(newProject);

        var httpResponse = await AuthenticatedClient.PostAsync("/project", content);

        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(newProject.Title, actualProject.Title);
        Assert.Equal(newProject.Description, actualProject.Description);
        Assert.Equal(newProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(newProject.CompanyId, actualProject.CompanyId);
        Assert.Equal(newProject.StartDate, actualProject.StartDate);
        Assert.Equal(newProject.EndDate, actualProject.EndDate);
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
        Assert.Equal(newProject.StartDate, projectInDb.StartDate);
        Assert.Equal(newProject.EndDate, projectInDb.EndDate);
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
    public async Task PostProject_WithImages_CreatesProjectAndImagesWithoutDuplicateInsertError()
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent("Project With Images"), "Title" },
            { new StringContent("A short description"), "ShortDescription" },
            { new StringContent("A long description"), "Description" },
            { new StringContent("2021-01-01"), "StartDate" },
            { new StringContent("2022-01-01"), "EndDate" }
        };
        content.Add(new StringContent(ImageType.Logo.ToString()), "Images[0].Type");
        content.Add(new StreamContent(new MemoryStream("logo"u8.ToArray())), "Images[0].Image", "logo.png");
        content.Add(new StringContent(ImageType.Banner.ToString()), "Images[1].Type");
        content.Add(new StreamContent(new MemoryStream("banner"u8.ToArray())), "Images[1].Image", "banner.png");

        var httpResponse = await AuthenticatedClient.PostAsync("/project", content);
        httpResponse.EnsureSuccessStatusCode();

        var actualProject = GetJsonContent<Project>(await httpResponse.Content.ReadAsStringAsync());
        Assert.NotNull(actualProject);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectCount = await context.Project.CountAsync(p => p.Id == actualProject.Id);
        Assert.Equal(1, projectCount);

        var images = await context.ProjectImage.Where(i => i.ProjectId == actualProject.Id).ToListAsync();
        Assert.Equal(2, images.Count);
    }

    [Fact]
    public async Task PostProject_WithoutAuth_ReturnsUnauthorized()
    {
        var newProject = new CreateProject
        {
            Title = "New Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = CompanyId.New(),
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"]
        };

        var content = GetMultipartFormDataContent(newProject);

        var httpResponse = await Client.PostAsync("/project", content);

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostProject_WithDuplicateTitle_ReturnsConflict()
    {
        var duplicateProject = new CreateProject
        {
            Title = "Duplicate Project",
            ShortDescription = "A short description",
            Description = "A long description",
            CompanyId = null,
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://example.com",
            Tags = ["Tag1", "Tag2"]
        };

        var content = GetMultipartFormDataContent(duplicateProject);

        // Act - First attempt (should succeed)
        var firstResponse = await AuthenticatedClient.PostAsync("/project", content);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Second attempt (should fail)
        var secondResponse = await AuthenticatedClient.PostAsync("/project", content);

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
        var testProjectId = ProjectId.New();
        var existingProject = await CreateProjectAsync(testProjectId);

        var updateProject = new CreateProject
        {
            Title = "Updated Project",
            ShortDescription = "Updated short description",
            Description = "Updated long description",
            CompanyId = null,
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://updated-example.com",
            Tags = ["UpdatedTag1", "UpdatedTag2"]
        };

        var content = GetMultipartFormDataContent(updateProject);

        var httpResponse = await AuthenticatedClient.PutAsync($"/project/{existingProject.Id}", content);

        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualProject = GetJsonContent<Project>(stringResponse);

        Assert.NotNull(actualProject);
        Assert.Equal(updateProject.Title, actualProject.Title);
        Assert.Equal(updateProject.Description, actualProject.Description);
        Assert.Equal(updateProject.ShortDescription, actualProject.ShortDescription);
        Assert.Equal(updateProject.CompanyId, actualProject.CompanyId);
        Assert.Equal(updateProject.StartDate, actualProject.StartDate);
        Assert.Equal(updateProject.EndDate, actualProject.EndDate);
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
        Assert.Equal(updateProject.StartDate, projectInDb.StartDate);
        Assert.Equal(updateProject.EndDate, projectInDb.EndDate);
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
        var testProjectId = ProjectId.New();
        var existingProject = await CreateProjectAsync(testProjectId);

        var updateProject = new CreateProject
        {
            Title = "Updated Project",
            ShortDescription = "Updated short description",
            Description = "Updated long description",
            CompanyId = CompanyId.New(),
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://updated-example.com",
            Tags = ["UpdatedTag1", "UpdatedTag2"]
        };

        var content = GetMultipartFormDataContent(updateProject);

        var httpResponse = await Client.PutAsync($"/project/{existingProject.Id}", content);

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PutProject_ReplacingBannerImageOnly_LeavesLogoAndCardUnaffected()
    {
        var testProjectId = ProjectId.New();
        var existingProject = await CreateProjectAsync(testProjectId, "Image Sync Project");

        var originalLogo = new ProjectImage { Image = "original-logo.png", Type = ImageType.Logo, ProjectId = testProjectId };
        var originalBanner = new ProjectImage { Image = "original-banner.png", Type = ImageType.Banner, ProjectId = testProjectId };
        var originalCard = new ProjectImage { Image = "original-card.png", Type = ImageType.Card, ProjectId = testProjectId };
        var originalDesktop1 = new ProjectImage { Image = "original-desktop1.png", Type = ImageType.Desktop, Order = 1, ProjectId = testProjectId };
        var originalDesktop2 = new ProjectImage { Image = "original-desktop2.png", Type = ImageType.Desktop, Order = 2, ProjectId = testProjectId };
        var originalMobile1 = new ProjectImage { Image = "original-mobile1.png", Type = ImageType.Mobile, Order = 1, ProjectId = testProjectId };
        var originalMobile2 = new ProjectImage { Image = "original-mobile2.png", Type = ImageType.Mobile, Order = 2, ProjectId = testProjectId };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Database>();
            context.ProjectImage.AddRange(originalLogo, originalBanner, originalCard, originalDesktop1, originalDesktop2, originalMobile1, originalMobile2);
            await context.SaveChangesAsync();
        }

        var updateContent = new MultipartFormDataContent
        {
            { new StringContent(existingProject.Title), "Title" },
            { new StringContent(existingProject.ShortDescription), "ShortDescription" },
            { new StringContent(existingProject.Description), "Description" },
            { new StringContent(existingProject.StartDate.ToString("yyyy-MM-dd")), "StartDate" },
            { new StringContent(existingProject.EndDate!.Value.ToString("yyyy-MM-dd")), "EndDate" }
        };
        updateContent.Add(new StringContent(originalLogo.Id.Value.ToString()), "Images[0].Id");
        updateContent.Add(new StringContent(ImageType.Logo.ToString()), "Images[0].Type");
        updateContent.Add(new StringContent(ImageType.Banner.ToString()), "Images[1].Type");
        updateContent.Add(new StreamContent(new MemoryStream("new-banner"u8.ToArray())), "Images[1].Image", "new-banner.png");
        updateContent.Add(new StringContent(originalCard.Id.Value.ToString()), "Images[2].Id");
        updateContent.Add(new StringContent(ImageType.Card.ToString()), "Images[2].Type");
        updateContent.Add(new StringContent(originalDesktop1.Id.Value.ToString()), "Images[3].Id");
        updateContent.Add(new StringContent(ImageType.Desktop.ToString()), "Images[3].Type");
        updateContent.Add(new StringContent("1"), "Images[3].Order");
        updateContent.Add(new StringContent(originalDesktop2.Id.Value.ToString()), "Images[4].Id");
        updateContent.Add(new StringContent(ImageType.Desktop.ToString()), "Images[4].Type");
        updateContent.Add(new StringContent("2"), "Images[4].Order");
        updateContent.Add(new StringContent(originalMobile1.Id.Value.ToString()), "Images[5].Id");
        updateContent.Add(new StringContent(ImageType.Mobile.ToString()), "Images[5].Type");
        updateContent.Add(new StringContent("1"), "Images[5].Order");
        updateContent.Add(new StringContent(originalMobile2.Id.Value.ToString()), "Images[6].Id");
        updateContent.Add(new StringContent(ImageType.Mobile.ToString()), "Images[6].Type");
        updateContent.Add(new StringContent("2"), "Images[6].Order");

        var updateResponse = await AuthenticatedClient.PutAsync($"/project/{testProjectId}", updateContent);
        updateResponse.EnsureSuccessStatusCode();

        var getResponse = await Client.GetAsync($"/project/{testProjectId}");
        getResponse.EnsureSuccessStatusCode();
        var refreshedProject = GetJsonContent<Project>(await getResponse.Content.ReadAsStringAsync());
        Assert.NotNull(refreshedProject);

        var refreshedLogo = refreshedProject.Images.SingleOrDefault(i => i.Type == ImageType.Logo);
        var refreshedBanner = refreshedProject.Images.SingleOrDefault(i => i.Type == ImageType.Banner);
        var refreshedCard = refreshedProject.Images.SingleOrDefault(i => i.Type == ImageType.Card);

        Assert.NotNull(refreshedLogo);
        Assert.Equal(originalLogo.Image, refreshedLogo.Image);

        Assert.NotNull(refreshedCard);
        Assert.Equal(originalCard.Image, refreshedCard.Image);

        Assert.NotNull(refreshedBanner);
        Assert.NotEqual(originalBanner.Image, refreshedBanner.Image);
        Assert.NotEqual(refreshedCard.Image, refreshedBanner.Image);

        Assert.Single(refreshedProject.Images, i => i.Type == ImageType.Logo);
        Assert.Single(refreshedProject.Images, i => i.Type == ImageType.Banner);
        Assert.Single(refreshedProject.Images, i => i.Type == ImageType.Card);
    }

    [Fact]
    public async Task DeleteProject_ExistingId_DeletesProject()
    {
        var testProjectId = ProjectId.New();
        var existingProject = await CreateProjectAsync(testProjectId);

        var httpResponse = await AuthenticatedClient.DeleteAsync($"/project/{existingProject.Id}");

        httpResponse.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();
        var projectInDb = await context.Project.FindAsync(existingProject.Id);
        Assert.Null(projectInDb);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_ReturnsUnauthorized()
    {
        var testProjectId = ProjectId.New();
        var existingProject = await CreateProjectAsync(testProjectId);

        var httpResponse = await Client.DeleteAsync($"/project/{existingProject.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }
}
