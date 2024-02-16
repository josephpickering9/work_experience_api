using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var projects = await projectService.GetProjectsAsync(search);
        return projects.ToResponse();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await projectService.GetProjectAsync(id);
        return project.ToResponse();
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Project>> GetProject(string slug)
    {
        var project = await projectService.GetProjectBySlugAsync(slug);
        return project.ToResponse();
    }

    [HttpGet("{id:int}/related")]
    public async Task<ActionResult<IEnumerable<Project>>> GetRelatedProjects(int id)
    {
        var projects = await projectService.GetRelatedProjectsAsync(id);
        return projects.ToResponse();
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PostProject([FromForm] CreateProject createProject)
    {
        var project = await projectService.CreateProjectAsync(createProject);
        if (!project.IsSuccess) return project.ToErrorResponse();

        return CreatedAtAction("GetProject", new { id = project.Data.Id }, project);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PutProject(int id, [FromForm] CreateProject createProject)
    {
        var project = await projectService.UpdateProjectAsync(id, createProject);
        return project.ToResponse();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await projectService.DeleteProjectAsync(id);
        return project.ToResponse();
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
