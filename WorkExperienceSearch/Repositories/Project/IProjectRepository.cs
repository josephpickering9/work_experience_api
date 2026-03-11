using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetByIdsAsync(IEnumerable<ProjectId> ids, CancellationToken cancellationToken = default);
    Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default);
}
