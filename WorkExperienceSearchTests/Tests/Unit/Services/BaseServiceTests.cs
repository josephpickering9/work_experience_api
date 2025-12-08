using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class BaseServiceTests : IAsyncLifetime
{
    protected readonly Database Context;
    protected static readonly CompanyId Company1Id = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    protected static readonly ProjectId Project1Id = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    protected static readonly ProjectId Project2Id = new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    protected static readonly ProjectId Project3Id = new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    protected static readonly TagId Tag1Id = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    protected static readonly TagId Tag2Id = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    protected static readonly TagId Tag3Id = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    protected static readonly ProjectImageId Image1Id = new(Guid.Parse("44444444-4444-4444-4444-444444444441"));
    protected static readonly ProjectImageId Image2Id = new(Guid.Parse("44444444-4444-4444-4444-444444444442"));
    protected static readonly ProjectImageId Image3Id = new(Guid.Parse("44444444-4444-4444-4444-444444444443"));
    protected static readonly ProjectImageId Image4Id = new(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    protected static readonly ProjectImageId Image5Id = new(Guid.Parse("44444444-4444-4444-4444-444444444445"));
    protected static readonly ProjectImageId Image6Id = new(Guid.Parse("44444444-4444-4444-4444-444444444446"));
    protected static readonly ProjectImageId Image7Id = new(Guid.Parse("44444444-4444-4444-4444-444444444447"));

    protected BaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseInMemoryDatabase($"UnitTests-{Guid.NewGuid()}")
            .Options;

        Context = new Database(options);
        Context.Database.EnsureCreated();
    }
    
    public async Task InitializeAsync()
    {
        await ClearDatabase();
        await SeedDatabase();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
    
    private async Task SeedDatabase()
    {
        Context.Tag.AddRange(await GetTestTags());
        Context.Company.AddRange(GetTestCompanies());
        Context.Project.AddRange(await GetTestProjects());
        Context.ProjectImage.AddRange(GetTestProjectImages());
        await Context.SaveChangesAsync();
    }

    protected async Task ClearDatabase()
    {
        Context.Project.RemoveRange(Context.Project);
        Context.Tag.RemoveRange(Context.Tag);
        Context.Company.RemoveRange(Context.Company);
        Context.ProjectImage.RemoveRange(Context.ProjectImage);
        Context.ProjectRepository.RemoveRange(Context.ProjectRepository);
        await Context.SaveChangesAsync();
    }

    protected static Tag CreateTag(TagId id, string title, TagType type, List<Project>? projects = null)
    {
        return new Tag
        {
            Id = id,
            Title = title,
            Type = type,
            Icon = "testIcon",
            CustomColour = null,
            Projects = projects ?? []
        };
    }

    protected static Company CreateCompany(
        CompanyId id,
        string name,
        string description,
        string logo,
        string website,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new Company
        {
            Id = id,
            Name = name,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Logo = logo,
            Website = website
        };
    }

    protected static Project CreateProject(
        ProjectId id,
        string title = "Title",
        string description = "Description",
        string shortDescription = "Short Description",
        CompanyId? companyId = null!,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string website = null!,
        List<Tag> tags = null!
    )
    {
        var resolvedStartDate = startDate ?? new DateOnly(2020, 1, 1);
        return new Project
        {
            Id = id,
            Title = title,
            Description = description,
            ShortDescription = shortDescription,
            CompanyId = companyId,
            StartDate = resolvedStartDate,
            EndDate = endDate ?? resolvedStartDate.AddYears(1),
            Website = website,
            Tags = tags,
            Slug = title.ToSlug()
        };
    }

    private static ProjectImage CreateProjectImage(
        Guid id,
        string image,
        ImageType type,
        int? order = null,
        Guid? projectId = null!
    ) =>
        CreateProjectImage(new ProjectImageId(id), image, type, order,
            projectId.HasValue ? new ProjectId(projectId.Value) : null);

    private static ProjectImage CreateProjectImage(
        ProjectImageId id,
        string image,
        ImageType type,
        int? order = null,
        ProjectId? projectId = null!
    )
    {
        var resolvedProjectId = projectId ?? Project1Id;
        return new ProjectImage
        {
            Id = id,
            Image = image,
            Type = type,
            Order = order,
            ProjectId = resolvedProjectId
        };
    }

    private async Task<IEnumerable<Tag>> GetTestTags()
    {
        var cSharpTag = CreateTag(Tag1Id, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(Tag2Id, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(Tag3Id, "Xamarin Forms", TagType.Frontend);

        List<Tag> tags = [cSharpTag, aspNetCoreTag, xamarinFormsTag];
        var returnTags = new List<Tag>();
        foreach (var tag in tags)
        {
            var existing = await Context.Tag.FindAsync(tag.Id);
            returnTags.Add(existing ?? tag);
        }

        return returnTags;
    }
    
    private static IEnumerable<Company> GetTestCompanies() =>
        [
            CreateCompany(
                Company1Id,
                "Test Company",
                "Test Description",
                "testLogo",
                "https://example.com",
                new DateOnly(2020, 1, 1),
                new DateOnly(2021, 1, 1))
        ];
    
    private async Task<IEnumerable<Project>> GetTestProjects()
    {
        var tags = (await GetTestTags()).ToList();
        var companies = GetTestCompanies().ToList();

        return
        [
            CreateProject(Project1Id, "Visit Northumberland",
                "A website for Visit Northumberland using C# and ASP.NET Core MVC.",
                "A website for Visit Northumberland", companies.First().Id,
                new DateOnly(2020, 1, 1), new DateOnly(2021, 1, 1), "https://visitnorthumberland.com/", [tags[0], tags[1]]),
            CreateProject(Project2Id, "BeatCovidNE", "A website for BeatCovidNE using C# and ASP.NET Core MVC.",
                "A website for BeatCovidNE", companies.First().Id, new DateOnly(2021, 1, 1),
                new DateOnly(2022, 1, 1),
                "https://beatcovidne.co.uk/", [tags[1], tags[2]]),
            CreateProject(Project3Id, "taxigoat",
                "A website & mobile application for taxigoat using Xamarin Forms and ASP.NET Core API.",
                "A website for taxigoat", companies.First().Id, new DateOnly(2019, 1, 1),
                new DateOnly(2020, 1, 1),
                "https://taxigoat.co.uk/", [tags[2]])
        ];
    }
    
    private IEnumerable<ProjectImage> GetTestProjectImages(ProjectId projectId = default)
    {
        projectId = projectId == default ? Project1Id : projectId;
        var testLogo = CreateProjectImage(Image1Id, "testLogo.png", ImageType.Logo, projectId: projectId);
        var testBanner = CreateProjectImage(Image2Id, "testBanner.png", ImageType.Banner, projectId: projectId);
        var testCard = CreateProjectImage(Image3Id, "testCard.png", ImageType.Card, projectId: projectId);
        var testDesktop1 = CreateProjectImage(Image4Id, "testDesktop1.png", ImageType.Desktop, 1, projectId);
        var testDesktop2 = CreateProjectImage(Image5Id, "testDesktop2.png", ImageType.Desktop, 2, projectId);
        var testMobile1 = CreateProjectImage(Image6Id, "testMobile1.png", ImageType.Mobile, 1, projectId);
        var testMobile2 = CreateProjectImage(Image7Id, "testMobile2.png", ImageType.Mobile, 2, projectId);

        return
        [
            testLogo,
            testBanner,
            testCard,
            testDesktop1,
            testDesktop2,
            testMobile1,
            testMobile2
        ];
    }
    
    protected async Task<Project> SaveProject(Project project)
    {
        await Context.Project.AddAsync(project);
        await Context.SaveChangesAsync();
        return project;
    }
}
