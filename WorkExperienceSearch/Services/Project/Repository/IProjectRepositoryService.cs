using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectRepositoryService
{
    Task<Result<IEnumerable<ProjectRepository>>> GetProjectRepositoriesAsync(int projectId);
    Task<Result<ProjectRepository>> GetProjectRepositoryAsync(int projectId, int id);
    Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(int projectId, List<CreateProjectRepository> repositories);
    Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(Models.Project project, List<CreateProjectRepository> repositories);
}
