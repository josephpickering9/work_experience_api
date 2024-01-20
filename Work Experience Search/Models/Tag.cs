namespace Work_Experience_Search.models;

public class Tag
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string Colour { get; set; }
    
    public List<Project> Projects { get; set; }
}