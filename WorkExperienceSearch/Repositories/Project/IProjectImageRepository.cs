using Work_Experience_Search.Models;

namespace Work_Experience_Search.Repositories;

public interface IProjectImageRepository
{
    Task AddAsync(ProjectImage image, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectImage image, CancellationToken cancellationToken = default);
    Task RemoveAsync(ProjectImage image, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectImage>> GetUnoptimisedImagesAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
