using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public class ProjectRepositoryService(Database context) : IProjectRepositoryService
{
    public async Task<Result<IEnumerable<ProjectRepository>>> GetProjectRepositoriesAsync(int projectId)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<IEnumerable<ProjectRepository>>("Project not found.");

        return new Success<IEnumerable<ProjectRepository>>(project.Repositories);
    }

    public async Task<Result<ProjectRepository>> GetProjectRepositoryAsync(int projectId, int id)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<ProjectRepository>("Project not found.");

        var repository = project.Repositories.SingleOrDefault(i => i.Id == id);
        if (repository == null) return new NotFoundFailure<ProjectRepository>("Repository not found.");

        return new Success<ProjectRepository>(repository);
    }

    public async Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(int projectId, List<CreateProjectRepository> repositories)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<List<ProjectRepository>>("Project not found.");

        return await SyncProjectRepositoriesAsync(project, repositories);
    }

    public async Task<Result<List<ProjectRepository>>> SyncProjectRepositoriesAsync(
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
            context.ProjectRepository.Remove(repository);
        }

        foreach (var repository in repositoriesToCreate)
        {
            var projectRepository = new ProjectRepository
            {
                Title = repository.Title,
                Url = repository.Url,
                Project = project
            };

            context.ProjectRepository.Add(projectRepository);
            await context.SaveChangesAsync();

            repositoriesToSave.Add(projectRepository);
        }

        if (repositoriesToCreate.Count > 0 || repositoriesToDelete.Count > 0) await context.SaveChangesAsync();

        return new Success<List<ProjectRepository>>(repositoriesToSave);
    }
}
