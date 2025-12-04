using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class BaseServiceTests : IAsyncLifetime
{
    protected readonly Database Context;
    protected static readonly Guid CompanyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    protected static readonly Guid Project1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    protected static readonly Guid Project2Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    protected static readonly Guid Project3Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    protected static readonly Guid Tag1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected static readonly Guid Tag2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    protected static readonly Guid Tag3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    protected static readonly Guid Image1Id = Guid.Parse("44444444-4444-4444-4444-444444444441");
    protected static readonly Guid Image2Id = Guid.Parse("44444444-4444-4444-4444-444444444442");
    protected static readonly Guid Image3Id = Guid.Parse("44444444-4444-4444-4444-444444444443");
    protected static readonly Guid Image4Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    protected static readonly Guid Image5Id = Guid.Parse("44444444-4444-4444-4444-444444444445");
    protected static readonly Guid Image6Id = Guid.Parse("44444444-4444-4444-4444-444444444446");
    protected static readonly Guid Image7Id = Guid.Parse("44444444-4444-4444-4444-444444444447");

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

    protected static Tag CreateTag(Guid id, string title, TagType type, List<Project>? projects = null)
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

    protected static Company CreateCompany(Guid id, string name, string description, string logo, string website)
    {
        return new Company
        {
            Id = id,
            Name = name,
            Description = description,
            Logo = logo,
            Website = website
        };
    }

    protected static Project CreateProject(
        Guid id,
        string title = "Title",
        string description = "Description",
        string shortDescription = "Short Description",
        Guid? companyId = null!,
        int year = 2020,
        string website = null!,
        List<Tag> tags = null!
    )
    {
        return new Project
        {
            Id = id,
            Title = title,
            Description = description,
            ShortDescription = shortDescription,
            CompanyId = companyId,
            Year = year,
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
    )
    {
        return new ProjectImage
        {
            Id = id,
            Image = image,
            Type = type,
            Order = order,
            ProjectId = projectId
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
    
    private static IEnumerable<Company> GetTestCompanies()
    {
        var company = CreateCompany(CompanyId, "Drummond Central", "A marketing agency based in Newcastle upon Tyne.",
            "https://drummondcentral.co.uk/wp-content/uploads/2019/10/DC-Logo-White.png",
            "https://drummondcentral.co.uk/");

        return [company];
    }
    
    private async Task<IEnumerable<Project>> GetTestProjects()
    {
        var tags = (await GetTestTags()).ToList();
        var companies = GetTestCompanies().ToList();

        return
        [
            CreateProject(Project1Id, "Visit Northumberland",
                "A website for Visit Northumberland using C# and ASP.NET Core MVC.",
                "A website for Visit Northumberland", companies.First().Id,
                2020, "https://visitnorthumberland.com/", [tags[0], tags[1]]),
            CreateProject(Project2Id, "BeatCovidNE", "A website for BeatCovidNE using C# and ASP.NET Core MVC.",
                "A website for BeatCovidNE", companies.First().Id, 2021,
                "https://beatcovidne.co.uk/", [tags[1], tags[2]]),
            CreateProject(Project3Id, "taxigoat",
                "A website & mobile application for taxigoat using Xamarin Forms and ASP.NET Core API.",
                "A website for taxigoat", companies.First().Id, 2019,
                "https://taxigoat.co.uk/", [tags[2]])
        ];
    }
    
    private IEnumerable<ProjectImage> GetTestProjectImages(Guid projectId = default)
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
