using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Work_Experience_Search.Models;

public class ProjectImage
{
    [Required] public int Id { get; set; }

    [Required] public string Image { get; set; } = null!;

    [Required] public ImageType Type { get; set; }

    public int? Order { get; set; }

    public int? ProjectId { get; set; }
    [Required] [JsonIgnore] public Project? Project { get; set; }
}

public enum ImageType
{
    [Description("Logo")] Logo, // 0
    [Description("Banner")] Banner, // 1
    [Description("Card")] Card, // 2
    [Description("Desktop")] Desktop, // 3
    [Description("Mobile")] Mobile // 4
}
