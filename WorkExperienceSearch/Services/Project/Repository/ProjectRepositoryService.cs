using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

using Work_Experience_Search.Repositories;

namespace Work_Experience_Search.Services;

public class ProjectRepositoryService(IProjectRepository projectRepository, IProjectCodeRepository projectCodeRepository) : IProjectRepositoryService
{
    public async Task<Result<IEnumerable<Models.ProjectRepository>>> GetProjectRepositoriesAsync(ProjectId projectId)
    {
        var project = await projectRepository.GetAsync(projectId);
        if (project == null) return new NotFoundFailure<IEnumerable<Models.ProjectRepository>>("Project not found.");

        return new Success<IEnumerable<Models.ProjectRepository>>(project.Repositories);
    }

    public async Task<Result<Models.ProjectRepository>> GetProjectRepositoryAsync(ProjectId projectId, ProjectRepositoryId id)
    {
        var project = await projectRepository.GetAsync(projectId);
        if (project == null) return new NotFoundFailure<Models.ProjectRepository>("Project not found.");

        var repository = project.Repositories.SingleOrDefault(i => i.Id == id);
        if (repository == null) return new NotFoundFailure<Models.ProjectRepository>("Repository not found.");

        return new Success<Models.ProjectRepository>(repository);
    }

    public async Task<Result<List<Models.ProjectRepository>>> SyncProjectRepositoriesAsync(ProjectId projectId, List<CreateProjectRepository> repositories)
    {
        var project = await projectRepository.GetAsync(projectId);
        if (project == null) return new NotFoundFailure<List<Models.ProjectRepository>>("Project not found.");

        return await SyncProjectRepositoriesAsync(project, repositories);
    }

    public async Task<Result<List<Models.ProjectRepository>>> SyncProjectRepositoriesAsync(
        Project project,
        List<CreateProjectRepository> repositories
    )
    {
        var repositoryIds = repositories.Select(i => i.Id).ToList();
        var repositoriesToDelete = project.Repositories.Where(i => !repositoryIds.Contains(i.Id)).ToList();
        var repositoriesToCreate = repositories.Where(i => i.Id == null).ToList();
        var repositoriesToSave = project.Repositories.Where(i => repositories.Any(x => x.Id == i.Id)).ToList();

        foreach (var repository in repositoriesToDelete)
        {
            await projectCodeRepository.RemoveAsync(repository);
        }

        foreach (var repository in repositoriesToCreate)
        {
            var projectRepository = new Models.ProjectRepository
            {
                Title = repository.Title,
                Url = repository.Url,
                Project = project
            };

            await projectCodeRepository.AddAsync(projectRepository);

            repositoriesToSave.Add(projectRepository);
        }

        if (repositoriesToCreate.Count > 0 || repositoriesToDelete.Count > 0) await projectCodeRepository.SaveChangesAsync();

        return new Success<List<Models.ProjectRepository>>(repositoriesToSave);
    }
}
