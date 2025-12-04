using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Work_Experience_Search.Models;

public class ProjectRepository
{
    [Required] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string Title { get; set; } = null!;

    [Required] public string Url { get; set; } = null!;

    public int? Order { get; set; }

    public Guid ProjectId { get; set; }
    [Required] [JsonIgnore] public Project? Project { get; set; }
}
