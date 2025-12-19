using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class TagRepository(Database context) : ITagRepository
{
    public async Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<TagId> ids, CancellationToken cancellationToken = default)
    {
        return await context.Tag
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await context.Tag
            .Include(t => t.Projects)
            .ThenInclude(p => p.Images)
            .Include(t => t.Projects)
            .ThenInclude(p => p.Tags)
            .SingleOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<Tag?> GetAsync(TagId id, CancellationToken cancellationToken = default)
    {
        return await context.Tag.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Tag?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
         return SupportsILike()
            ? await context.Tag.FirstOrDefaultAsync(t => EF.Functions.ILike(title, t.Title), cancellationToken)
            : await context.Tag.FirstOrDefaultAsync(t => t.Title != null && t.Title.Equals(title, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<IEnumerable<Tag>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        IQueryable<Tag> tags = context.Tag;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            tags = SupportsILike()
                ? tags.Where(p => EF.Functions.ILike(p.Title, $"%{search}%"))
                : tags.Where(p => p.Title != null && p.Title.ToLower().Contains(normalizedSearch));
        }

        return await tags.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string title, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Tag.AnyAsync(p => EF.Functions.ILike(p.Title, title), cancellationToken)
            : await context.Tag.AnyAsync(p => p.Title != null && p.Title.Equals(title, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string title, TagId excludeId, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Tag.AnyAsync(t => t.Id != excludeId && EF.Functions.ILike(t.Title, title), cancellationToken)
            : await context.Tag.AnyAsync(t => t.Id != excludeId && t.Title != null && t.Title.Equals(title, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        context.Tag.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        // Entity is tracked, just save changes
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        context.Tag.Remove(tag);
        await context.SaveChangesAsync(cancellationToken);
    }
    
    private bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
