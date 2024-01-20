namespace Work_Experience_Search.models;

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
    public string Company { get; set; }
    public string? Image { get; set; }
    public string? BackgroundImage { get; set; }
    public int Year { get; set; }
    public string? Website { get; set; }
    
    public List<Tag> Tags { get; set; }
}