using System.ComponentModel.DataAnnotations;

namespace Work_Experience_Search.models;

public class Project
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string ShortDescription { get; set; } = null!;
    [Required]
    public string Description { get; set; } = null!;
    [Required]
    public string Company { get; set; } = null!;
    public string? Image { get; set; }
    public string? BackgroundImage { get; set; }
    [Required]
    public int Year { get; set; }
    public string? Website { get; set; }
    [Required]
    public List<Tag> Tags { get; set; }
}