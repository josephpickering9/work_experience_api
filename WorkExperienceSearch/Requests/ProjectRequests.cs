using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Requests;

public record CreateProject
{
    public required string Title { get; init; }
    public required string ShortDescription { get; init; }
    public required string Description { get; init; }
    public CompanyId? CompanyId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Website { get; init; }
    public bool ShowMockup { get; init; } = false;
    public List<CreateProjectImage> Images { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public List<CreateProjectRepository> Repositories { get; init; } = [];
}

public record CreateProjectImage
{
    public ProjectImageId? Id { get; init; }
    public IFormFile? Image { get; init; }
    public required ImageType Type { get; init; }
    public int? Order { get; init; }
}

public record CreateProjectRepository
{
    public ProjectRepositoryId? Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public int? Order { get; init; }
}
