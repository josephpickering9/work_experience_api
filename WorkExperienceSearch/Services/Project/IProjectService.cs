using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectService
{
    Task<Result<IEnumerable<Project>>> GetProjectsAsync(string? search);
    Task<Result<Project>> GetProjectAsync(ProjectId id);
    Task<Result<Project>> GetProjectBySlugAsync(string slug);
    Task<Result<IEnumerable<Project>>> GetRelatedProjectsAsync(ProjectId projectId);
    Task<Result<Project>> CreateProjectAsync(CreateProject createProject);
    Task<Result<Project>> UpdateProjectAsync(ProjectId id, CreateProject createProject);
    Task<Result<Project>> DeleteProjectAsync(ProjectId id);
}
