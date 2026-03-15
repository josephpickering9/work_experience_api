using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class CompanyRepository(Database context, IMemoryCache cache, CacheInvalidator cacheInvalidator)
    : BaseRepository(context, cache), ICompanyRepository
{
    public async Task<IEnumerable<Company>> GetByIdsAsync(IEnumerable<CompanyId> ids, CancellationToken cancellationToken = default)
    {
        return await context.Company
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Company?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"company:slug:{slug}";
        if (TryGetCache(cacheKey, out Company? cached)) return cached;

        var company = await context.Company.SingleOrDefaultAsync(c => c.Slug == slug, cancellationToken);
        SetCache(cacheKey, company, cacheInvalidator.GetCompaniesChangeToken());
        return company;
    }

    public async Task<Company?> GetAsync(CompanyId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"company:{id}";
        if (TryGetCache(cacheKey, out Company? cached)) return cached;

        var company = await context.Company.FindAsync(new object[] { id }, cancellationToken);
        SetCache(cacheKey, company, cacheInvalidator.GetCompaniesChangeToken());
        return company;
    }

    public async Task<IEnumerable<Company>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"companies:{search ?? ""}";
        if (TryGetCache(cacheKey, out IEnumerable<Company>? cached) && cached != null) return cached;

        IQueryable<Company> companies = context.Company;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            companies = SupportsILike()
                ? companies.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"))
                : companies.Where(c => c.Name != null && c.Name.ToLower().Contains(normalizedSearch));
        }

        var result = await companies.ToListAsync(cancellationToken);
        SetCache(cacheKey, result, cacheInvalidator.GetCompaniesChangeToken());
        return result;
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
        cacheInvalidator.InvalidateCompanies();
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateCompanies();
    }

    public async Task DeleteAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Company.Remove(company);
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateCompanies();
    }
}
