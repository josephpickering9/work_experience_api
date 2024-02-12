using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync(string? search);
    Task<Project> GetProjectAsync(int id);
    Task<Project> GetProjectBySlugAsync(string slug);
    Task<IEnumerable<Project>> GetRelatedProjectsAsync(int projectId);
    Task<Project> CreateProjectAsync(CreateProject createProject);
    Task<Project> UpdateProjectAsync(int id, CreateProject createProject);
    Task<Project> DeleteProjectAsync(int id);
}
