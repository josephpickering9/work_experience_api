using System.ComponentModel.DataAnnotations;

namespace Work_Experience_Search.Models;

public class Company
{
    [Required] public int Id { get; set; }

    [Required] public string Name { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public string? Website { get; set; } = null!;

    public string? Logo { get; set; } = null!;
}
