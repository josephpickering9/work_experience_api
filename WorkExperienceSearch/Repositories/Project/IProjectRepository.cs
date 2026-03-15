using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetByIdsAsync(IEnumerable<ProjectId> ids, CancellationToken cancellationToken = default);
    Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default);
    Task<Project?> GetAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetRelatedAsync(ProjectId projectId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
