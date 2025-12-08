using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Models;

public class Tag
{
    [Required] public TagId Id { get; set; } = TagId.New();

    [Required] public string Title { get; set; } = null!;

    [Required] public TagType Type { get; set; }

    public string? Icon { get; set; }

    public string? CustomColour { get; set; }

    [Required] public string Slug { get; set; } = Guid.NewGuid().ToString();

    [Required] [JsonIgnore] public List<Project> Projects { get; set; } = [];
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
    [Description("Mobile")] Mobile, // 7
    [Description("AI")] AI // 8
}
