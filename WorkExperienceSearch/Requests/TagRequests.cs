using Work_Experience_Search.Models;

namespace Work_Experience_Search.Requests;

public record CreateTag
{
    public required string Title { get; init; }
    public required TagType Type { get; init; }
    public string? Icon { get; init; }
    public string? CustomColour { get; init; }
}
