using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Tests;

public class BaseServiceTests
{
    protected readonly Database _context;

    protected BaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseInMemoryDatabase($"TestDatabase-{Guid.NewGuid()}")
            .Options;

        _context = new Database(options);
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
            Projects = projects ?? new List<Project>()
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
        int companyId = 1,
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
}