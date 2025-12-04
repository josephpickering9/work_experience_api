using System.ComponentModel.DataAnnotations;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Models;

public class Company
{
    [Required] public CompanyId Id { get; set; } = CompanyId.New();

    [Required] public string Name { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public string? Website { get; set; } = null!;

    public string? Logo { get; set; } = null!;

    [Required] public string Slug { get; set; } = Guid.NewGuid().ToString();
}
