using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Models;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Project>> GetProject(ProjectId id)
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

    [HttpGet("{id:guid}/related")]
    public async Task<ActionResult<IEnumerable<Project>>> GetRelatedProjects(ProjectId id)
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
        return project.ToResponse();
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Project>> PutProject(ProjectId id, [FromForm] CreateProject createProject)
    {
        var project = await projectService.UpdateProjectAsync(id, createProject);
        return project.ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject(ProjectId id)
    {
        var project = await projectService.DeleteProjectAsync(id);
        return project.ToResponse();
    }
}
