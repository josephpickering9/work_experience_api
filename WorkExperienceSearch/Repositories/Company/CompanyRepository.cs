using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class CompanyRepository(Database context) : ICompanyRepository
{
    public async Task<IEnumerable<Company>> GetByIdsAsync(IEnumerable<CompanyId> ids, CancellationToken cancellationToken = default)
    {
        return await context.Company
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Company?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Company
            .SingleOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<Company?> GetAsync(CompanyId id, CancellationToken cancellationToken = default)
    {
        return await context.Company.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Company>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        IQueryable<Company> companies = context.Company;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            companies = SupportsILike()
                ? companies.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"))
                : companies.Where(c => c.Name != null && c.Name.ToLower().Contains(normalizedSearch));
        }

        return await companies.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Company.AnyAsync(c => EF.Functions.ILike(c.Name, name), cancellationToken)
            : await context.Company.AnyAsync(c => c.Name != null && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, CompanyId excludeId, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Company.AnyAsync(p => p.Id != excludeId && EF.Functions.ILike(p.Name, name), cancellationToken)
            : await context.Company.AnyAsync(p => p.Id != excludeId && p.Name != null && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Company.Add(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        // Entity is tracked, just save changes
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Company.Remove(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    private bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
