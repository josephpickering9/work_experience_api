namespace Work_Experience_Search.controllers;

using System.ComponentModel.DataAnnotations;
using Services;
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
        
        project.Tags = await _context.Tag.Where(t => t.Projects.Any(p => p.Id == project.Id)).ToListAsync();

        return project;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PostProject([FromForm] CreateProject createProject)
    {
        bool projectExists = await _context.Project
            .AnyAsync(p => p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists)
        {
            return Conflict("A project with the same title already exists.");
        }
        
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

        if (createProject.Tags != null && createProject.Tags.Count > 0)
        {
            foreach (string tagTitle in createProject.Tags)
            {
                Tag? tag = await _context.Tag.FirstOrDefaultAsync(t => t.Title.ToLower() == tagTitle.ToLower());

                if (tag == null)
                {
                    tag = new Tag
                    {
                        Title = tagTitle,
                        Type = TagType.Default,
                        Colour = "blue",
                    };

                    _context.Tag.Add(tag);
                }

                project.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
        }

        return CreatedAtAction("GetProject", new { id = project.Id }, project);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Project>> PutProject(int id, [FromForm] CreateProject createProject)
    {
        Project? project = await _context.Project.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }
        
        string? imagePath = createProject.Image != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image)) : null;
        string? bgImagePath = createProject.BackgroundImage != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage)) : null;
        
        if (imagePath != null)
        {
            project.Image = imagePath;
        }
        
        if (bgImagePath != null)
        {
            project.BackgroundImage = bgImagePath;
        }
        
        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.Company = createProject.Company;
        project.Year = createProject.Year;
        project.Website = createProject.Website;
        
        _context.Entry(project).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetProject", new { id = project.Id }, project);
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
    [Required]
    public string Title { get; set; }
    [Required]
    public string ShortDescription { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public string Company { get; set; }
    [Required]
    public int Year { get; set; }
    public string Website { get; set; }
    
    public IFormFile? Image { get; set; }
    public IFormFile? BackgroundImage { get; set; }
    
    [Required]
    public List<string> Tags { get; set; }
}
