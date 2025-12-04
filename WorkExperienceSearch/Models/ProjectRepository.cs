using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Models;

public class ProjectRepository
{
    [Required] public ProjectRepositoryId Id { get; set; } = ProjectRepositoryId.New();

    [Required] public string Title { get; set; } = null!;

    [Required] public string Url { get; set; } = null!;

    public int? Order { get; set; }

    public ProjectId ProjectId { get; set; }
    [Required] [JsonIgnore] public Project? Project { get; set; }
}
