using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Work_Experience_Search.Models;

public class Project
{
    [Required] public int Id { get; set; }

    [Required] public string Title { get; set; } = null!;

    [Required] public string ShortDescription { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public int? CompanyId { get; set; } = null!;
    public Company? Company { get; set; } = null!;

    [Required] public int Year { get; set; }

    public string? Website { get; set; }

    [Required] public string Slug { get; set; } = Guid.NewGuid().ToString();

    [Required] public List<ProjectImage> Images { get; set; } = new();

    [Required] public List<Tag> Tags { get; set; } = new();

    [NotMapped] public List<Project> RelatedProjects { get; set; } = new();

    [JsonIgnore] [NotMapped] public ProjectImage? Logo => Images.SingleOrDefault(i => i.Type == ImageType.Logo);
    [NotMapped] public string? LogoUrl => Logo?.Image;

    [JsonIgnore] [NotMapped] public ProjectImage? Card => Images.SingleOrDefault(i => i.Type == ImageType.Card);
    [NotMapped] public string? CardUrl => Card?.Image;

    [JsonIgnore] [NotMapped] public ProjectImage? Banner => Images.SingleOrDefault(i => i.Type == ImageType.Banner);
    [NotMapped] public string? BannerUrl => Banner?.Image;
}
