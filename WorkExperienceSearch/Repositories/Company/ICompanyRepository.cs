using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public interface ICompanyRepository
{
    Task<IEnumerable<Company>> GetByIdsAsync(IEnumerable<CompanyId> ids, CancellationToken cancellationToken = default);
    Task<Company?> GetAsync(string slug, CancellationToken cancellationToken = default);
    Task<Company?> GetAsync(CompanyId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Company>> SearchAsync(string? search, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CompanyId excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteAsync(Company company, CancellationToken cancellationToken = default);
}
