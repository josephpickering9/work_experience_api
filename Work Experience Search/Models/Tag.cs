using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Work_Experience_Search.Models;

public class Tag
{
    [Required] public int Id { get; set; }

    [Required] public string Title { get; set; } = null!;

    [Required] public TagType Type { get; set; }

    public string? Icon { get; set; } = null!;

    public string? CustomColour { get; set; } = null!;

    [Required] public List<Project> Projects { get; set; }
}

public enum TagType
{
    [Description("Default")] Default, // 0
    [Description("Backend")] Backend, // 1
    [Description("Frontend")] Frontend, // 2
    [Description("DevOps")] DevOps, // 3
    [Description("Other")] Other, // 4
    [Description("Data")] Data, // 5
    [Description("CMS")] CMS, // 6
    [Description("Mobile")] Mobile // 7
}
