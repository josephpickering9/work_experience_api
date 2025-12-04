using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectRepositoryService
{
    Task<Result<IEnumerable<ProjectRepository>>> GetProjectRepositoriesAsync(Guid projectId);
    Task<Result<ProjectRepository>> GetProjectRepositoryAsync(Guid projectId, Guid id);
    Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(Guid projectId, List<CreateProjectRepository> repositories);
    Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(Models.Project project, List<CreateProjectRepository> repositories);
}
