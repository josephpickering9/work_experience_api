using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectService
{
    Task<Result<IEnumerable<Project>>> GetProjectsAsync(string? search);
    Task<Result<Project>> GetProjectAsync(int id);
    Task<Result<Project>> GetProjectBySlugAsync(string slug);
    Task<Result<IEnumerable<Project>>> GetRelatedProjectsAsync(int projectId);
    Task<Result<Project>> CreateProjectAsync(CreateProject createProject);
    Task<Result<Project>> UpdateProjectAsync(int id, CreateProject createProject);
    Task<Result<Project>> DeleteProjectAsync(int id);
}
