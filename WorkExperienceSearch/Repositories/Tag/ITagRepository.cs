using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<TagId> ids, CancellationToken cancellationToken = default);
    Task<Tag?> GetAsync(string slug, CancellationToken cancellationToken = default);
    Task<Tag?> GetAsync(TagId id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string title, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string title, TagId excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default);
}
