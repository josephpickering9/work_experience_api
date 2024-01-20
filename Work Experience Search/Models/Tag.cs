using System.ComponentModel.DataAnnotations;

namespace Work_Experience_Search.models;

public class Tag
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Type { get; set; } = null!;
    [Required]
    public string Colour { get; set; } = null!;
    [Required]
    public List<Project> Projects { get; set; }
}