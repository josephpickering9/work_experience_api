using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(string? search)
    {
        return Ok(await _projectService.GetProjectsAsync(search));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        try
        {
            return await _projectService.GetProjectAsync(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Project>> GetProject(string slug)
    {
        try
        {
            return await _projectService.GetProjectBySlugAsync(slug);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PostProject([FromForm] CreateProject createProject)
    {
        try
        {
            var project = await _projectService.CreateProjectAsync(createProject);
            return CreatedAtAction("GetProject", new { id = project.Id }, project);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PutProject(int id, [FromForm] CreateProject createProject)
    {
        try
        {
            var project = await _projectService.UpdateProjectAsync(id, createProject);
            return CreatedAtAction("GetProject", new { id = project.Id }, project);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}

public class CreateProject
{
    [Required] public string Title { get; set; } = null!;

    [Required] public string ShortDescription { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public int? CompanyId { get; set; }

    [Required] public int Year { get; set; }

    public string? Website { get; set; }

    public List<CreateProjectImage> Images { get; set; } = new();

    [Required] public List<string> Tags { get; set; } = new();
}

public class CreateProjectImage
{
    public int? Id { get; set; }

    public IFormFile? Image { get; set; }

    [Required] public ImageType Type { get; set; }
}
