using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public interface IProjectCodeRepository
{
    Task AddAsync(Models.ProjectRepository repository, CancellationToken cancellationToken = default);
    Task RemoveAsync(Models.ProjectRepository repository, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
