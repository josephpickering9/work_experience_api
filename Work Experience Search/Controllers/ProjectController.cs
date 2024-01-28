using System.ComponentModel.DataAnnotations;
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

    [HttpGet("id")]
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

    [HttpPost]
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

    [HttpDelete("id")]
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
    [Required] public string Title { get; set; }

    [Required] public string ShortDescription { get; set; }

    [Required] public string Description { get; set; }

    public int? CompanyId { get; set; }

    [Required] public int Year { get; set; }

    public string? Website { get; set; }

    public IFormFile? Image { get; set; }
    public IFormFile? BackgroundImage { get; set; }

    [Required] public List<string> Tags { get; set; }
}
