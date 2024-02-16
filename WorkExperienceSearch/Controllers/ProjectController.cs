using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(string? search)
    {
        return Ok(await projectService.GetProjectsAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        try
        {
            return await projectService.GetProjectAsync(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Project>> GetProject(string slug)
    {
        try
        {
            return await projectService.GetProjectBySlugAsync(slug);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{id:int}/related")]
    public async Task<ActionResult<IEnumerable<Project>>> GetRelatedProjects(int id)
    {
        try
        {
            return Ok(await projectService.GetRelatedProjectsAsync(id));
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
            var project = await projectService.CreateProjectAsync(createProject);
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

    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PutProject(int id, [FromForm] CreateProject createProject)
    {
        try
        {
            var project = await projectService.UpdateProjectAsync(id, createProject);
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

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject(int id)
    {
        try
        {
            await projectService.DeleteProjectAsync(id);
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

    [Required] public bool ShowMockup { get; set; } = false;

    public List<CreateProjectImage> Images { get; set; } = [];

    [Required] public List<string> Tags { get; set; } = [];
}

public class CreateProjectImage
{
    public int? Id { get; set; }

    public IFormFile? Image { get; set; }

    [Required] public ImageType Type { get; set; }

    public int? Order { get; set; }
}
