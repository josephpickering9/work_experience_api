using Work_Experience_Search.Services;

namespace Work_Experience_Search.controllers;

using Microsoft.EntityFrameworkCore;
using models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class ProjectController : ControllerBase
{
    private readonly Database _context;
    private readonly IFileService _fileService;

    public ProjectController(Database context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(string search)
    {
        IQueryable<Project> projects = _context.Project;

        if (!String.IsNullOrEmpty(search))
        {
            projects = projects.Where(p => p.Title.ToLower().Contains(search.ToLower()) || p.Description.ToLower().Contains(search.ToLower()));
        }

        return await projects.ToListAsync();
    }
    
    [HttpGet("id")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        Project? project = await _context.Project.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        return project;
    }

    [HttpPost]
    public async Task<ActionResult<Project>> PostProject([FromForm] CreateProject createProject)
    {
        string? imagePath = createProject.Image != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image)) : null;
        string? bgImagePath = createProject.BackgroundImage != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage)) : null;
        
        Project project = new Project
        {
            Title = createProject.Title,
            ShortDescription = createProject.ShortDescription,
            Description = createProject.Description,
            Company = createProject.Company,
            Image = imagePath,
            BackgroundImage = bgImagePath,
            Year = createProject.Year,
            Website = createProject.Website,
        };
        
        _context.Project.Add(project);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetProject", new { id = project.Id }, project);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProject(int id, Project project)
    {
        if (id != project.Id)
        {
            return BadRequest();
        }

        _context.Entry(project).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpDelete("id")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        Project? project = await _context.Project.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        _context.Project.Remove(project);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateProject
{
    public string Title { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }
    public string Company { get; set; }
    public int Year { get; set; }
    public string Website { get; set; }
    
    public IFormFile? Image { get; set; }
    public IFormFile? BackgroundImage { get; set; }
    
    public List<string> Tags { get; set; }
}
