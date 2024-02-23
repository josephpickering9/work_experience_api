using Microsoft.EntityFrameworkCore;
using Npgsql;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class BaseServiceTests : IAsyncLifetime
{
    protected readonly Database Context;
    private string TestDatabaseName { get; set; }
    private string? ConnectionString { get; set; }

    protected BaseServiceTests()
    {
        TestDatabaseName = $"TestDatabase-{Guid.NewGuid()}";
        CreateTestDatabase();
        
        var options = new DbContextOptionsBuilder<Database>()
            .UseNpgsql(ConnectionString)
            .Options;

        Context = new Database(options);
        Context.Database.Migrate();
    }
    
    public async Task InitializeAsync()
    {
        await ClearDatabase();
        await SeedDatabase();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        DeleteTestDatabase();
    }

    private void CreateTestDatabase()
    {
        var masterConnectionString = GetConnectionString("postgres");

        using (var conn = new NpgsqlConnection(masterConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"CREATE DATABASE \"{TestDatabaseName}\"", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        ConnectionString = GetConnectionString(TestDatabaseName);
    }

    private void DeleteTestDatabase()
    {
        var masterConnectionString = GetConnectionString("postgres");

        using (var conn = new NpgsqlConnection(masterConnectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand($"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{TestDatabaseName}'", conn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand($"DROP DATABASE \"{TestDatabaseName}\"", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        NpgsqlConnection.ClearAllPools();
    }
    
    private static string GetConnectionString(string databaseName)
    {
        return $"Host=localhost;Port=5433;Database={databaseName};Username=testuser;Password=testpassword";
    }
    
    private async Task SeedDatabase()
    {
        Context.Tag.AddRange(await GetTestTags());
        Context.Company.AddRange(GetTestCompanies());
        Context.Project.AddRange(await GetTestProjects());
        Context.ProjectImage.AddRange(GetTestProjectImages());
        await Context.SaveChangesAsync();
        
        await Context.Database.ExecuteSqlRawAsync("SELECT setval('\"Tag_Id_seq\"', (SELECT max(\"Id\") FROM \"Tag\"));");
        await Context.Database.ExecuteSqlRawAsync("SELECT setval('\"Project_Id_seq\"', (SELECT max(\"Id\") FROM \"Project\"));");
        await Context.Database.ExecuteSqlRawAsync("SELECT setval('\"Company_Id_seq\"', (SELECT max(\"Id\") FROM \"Company\"));");
        await Context.Database.ExecuteSqlRawAsync("SELECT setval('\"ProjectImage_Id_seq\"', (SELECT max(\"Id\") FROM \"ProjectImage\"));");
        await Context.Database.ExecuteSqlRawAsync("SELECT setval('\"ProjectRepository_Id_seq\"', (SELECT max(\"Id\") FROM \"ProjectRepository\"));");
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

    protected static Tag CreateTag(int id, string title, TagType type, List<Project>? projects = null)
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

    protected static Company CreateCompany(int id, string name, string description, string logo, string website)
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
        int id,
        string title = "Title",
        string description = "Description",
        string shortDescription = "Short Description",
        int? companyId = null!,
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
        int id,
        string image,
        ImageType type,
        int? order = null,
        int? projectId = null!
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
        var cSharpTag = CreateTag(1, "C#", TagType.Backend);
        var aspNetCoreTag = CreateTag(2, "ASP.NET Core", TagType.Backend);
        var xamarinFormsTag = CreateTag(3, "Xamarin Forms", TagType.Frontend);

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
        var company = CreateCompany(1, "Drummond Central", "A marketing agency based in Newcastle upon Tyne.",
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
            CreateProject(1, "Visit Northumberland",
                "A website for Visit Northumberland using C# and ASP.NET Core MVC.",
                "A website for Visit Northumberland", companies.First().Id,
                2020, "https://visitnorthumberland.com/", [tags[0], tags[1]]),
            CreateProject(2, "BeatCovidNE", "A website for BeatCovidNE using C# and ASP.NET Core MVC.",
                "A website for BeatCovidNE", companies.First().Id, 2021,
                "https://beatcovidne.co.uk/", [tags[1], tags[2]]),
            CreateProject(3, "taxigoat",
                "A website & mobile application for taxigoat using Xamarin Forms and ASP.NET Core API.",
                "A website for taxigoat", companies.First().Id, 2019,
                "https://taxigoat.co.uk/", [tags[2]])
        ];
    }
    
    private IEnumerable<ProjectImage> GetTestProjectImages(int projectId = 1)
    {
        var testLogo = CreateProjectImage(1, "testLogo.png", ImageType.Logo, projectId: projectId);
        var testBanner = CreateProjectImage(2, "testBanner.png", ImageType.Banner, projectId: projectId);
        var testCard = CreateProjectImage(3, "testCard.png", ImageType.Card, projectId: projectId);
        var testDesktop1 = CreateProjectImage(4, "testDesktop1.png", ImageType.Desktop, 1, projectId);
        var testDesktop2 = CreateProjectImage(5, "testDesktop2.png", ImageType.Desktop, 2, projectId);
        var testMobile1 = CreateProjectImage(6, "testMobile1.png", ImageType.Mobile, 1, projectId);
        var testMobile2 = CreateProjectImage(7, "testMobile2.png", ImageType.Mobile, 2, projectId);

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