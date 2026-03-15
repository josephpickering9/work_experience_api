namespace Work_Experience_Search.Requests;

public record CreateCompany
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public IFormFile? Logo { get; init; }
    public string? Website { get; init; }
}
