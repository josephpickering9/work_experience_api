using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class ProjectRepository(Database context, IMemoryCache cache, CacheInvalidator cacheInvalidator) : IProjectRepository
{
    public async Task<IEnumerable<Project>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"projects:{search ?? ""}";
        if (cache.TryGetValue(cacheKey, out IEnumerable<Project>? cached) && cached != null)
            return cached;

        IQueryable<Project> projects = context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .Include(p => p.Repositories.OrderBy(i => i.Order ?? 0));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            projects = SupportsILike()
                ? projects.Where(p => EF.Functions.ILike(p.Title, $"%{search}%") || EF.Functions.ILike(p.ShortDescription, $"%{search}%"))
                : projects.Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(normalizedSearch)) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(normalizedSearch)));
        }

        var result = await projects.OrderByDescending(p => p.StartDate).ThenByDescending(p => p.EndDate).ToListAsync(cancellationToken);
        cache.Set(cacheKey, result, new MemoryCacheEntryOptions().AddExpirationToken(cacheInvalidator.GetProjectsChangeToken()));
        return result;
    }

    public async Task<IEnumerable<Project>> GetByIdsAsync(IEnumerable<ProjectId> ids, CancellationToken cancellationToken = default)
    {
        return await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .Include(p => p.Repositories)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetAsync(ProjectId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:{id}";
        if (cache.TryGetValue(cacheKey, out Project? cached))
            return cached;

        var project = await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .Include(p => p.Repositories)
            .Include(p => p.Company)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        cache.Set(cacheKey, project, new MemoryCacheEntryOptions().AddExpirationToken(cacheInvalidator.GetProjectsChangeToken()));
        return project;
    }

    public async Task<Project?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:slug:{slug}";
        if (cache.TryGetValue(cacheKey, out Project? cached))
            return cached;

        var project = await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .Include(p => p.Repositories.OrderBy(i => i.Order ?? 0))
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        cache.Set(cacheKey, project, new MemoryCacheEntryOptions().AddExpirationToken(cacheInvalidator.GetProjectsChangeToken()));
        return project;
    }

    public async Task<IEnumerable<Project>> GetRelatedAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:related:{projectId}";
        if (cache.TryGetValue(cacheKey, out IEnumerable<Project>? cached) && cached != null)
            return cached;

        var projectTags = context.Project
            .Include(pt => pt.Tags)
            .Where(pt => pt.Id == projectId)
            .SelectMany(pt => pt.Tags.Select(t => t.Id));

        if (!projectTags.Any())
            return [];

        var related = await context.Project
            .Where(p => p.Id != projectId && p.Tags.Any(t => projectTags.Contains(t.Id)))
            .Select(p => new
            {
                Project = p,
                SharedTagsCount = p.Tags.Count(t => projectTags.Contains(t.Id))
            })
            .OrderByDescending(x => x.SharedTagsCount)
            .Take(3)
            .Select(x => x.Project)
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .ToListAsync(cancellationToken);

        cache.Set(cacheKey, related, new MemoryCacheEntryOptions().AddExpirationToken(cacheInvalidator.GetProjectsChangeToken()));
        return related;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateProjects();
    }

    private bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
