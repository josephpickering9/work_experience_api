using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Repositories;

public class ProjectCodeRepository(Database context) : IProjectCodeRepository
{
    public async Task AddAsync(Models.ProjectRepository repository, CancellationToken cancellationToken = default)
    {
        context.ProjectRepository.Add(repository);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Models.ProjectRepository repository, CancellationToken cancellationToken = default)
    {
        context.ProjectRepository.Remove(repository);
        await context.SaveChangesAsync(cancellationToken);
    }

    // Sometimes we might want to just save changes if we modified tracked entities
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
