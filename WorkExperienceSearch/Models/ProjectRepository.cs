using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Work_Experience_Search.Models;

public class ProjectRepository
{
    [Required] public int Id { get; set; }

    [Required] public string Title { get; set; } = null!;

    [Required] public string Url { get; set; } = null!;

    public int? Order { get; set; }

    public int? ProjectId { get; set; }
    [Required] [JsonIgnore] public Project? Project { get; set; }
}
