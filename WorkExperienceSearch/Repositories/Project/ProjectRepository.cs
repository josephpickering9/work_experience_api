using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class ProjectRepository(Database context, IMemoryCache cache, CacheInvalidator cacheInvalidator)
    : BaseRepository(context, cache), IProjectRepository
{
    public async Task<IEnumerable<Project>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"projects:{search ?? ""}";
        if (TryGetCache(cacheKey, out IEnumerable<Project>? cached) && cached != null) return cached;

        var projects = WithIncludes(context.Project);

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
        SetCache(cacheKey, result, cacheInvalidator.GetProjectsChangeToken());
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
        if (TryGetCache(cacheKey, out Project? cached)) return cached;

        var project = await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .Include(p => p.Repositories)
            .Include(p => p.Company)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        SetCache(cacheKey, project, cacheInvalidator.GetProjectsChangeToken());
        return project;
    }

    public async Task<Project?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:slug:{slug}";
        if (TryGetCache(cacheKey, out Project? cached)) return cached;

        var project = await WithIncludes(context.Project)
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        SetCache(cacheKey, project, cacheInvalidator.GetProjectsChangeToken());
        return project;
    }

    public async Task<Project?> GetForUpdateAsync(ProjectId id, CancellationToken cancellationToken = default)
    {
        return await WithIncludes(context.Project)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetRelatedAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:related:{projectId}";
        if (TryGetCache(cacheKey, out IEnumerable<Project>? cached) && cached != null) return cached;

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

        SetCache(cacheKey, related, cacheInvalidator.GetProjectsChangeToken());
        return related;
    }

    public async Task<bool> ExistsAsync(string title, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Project.AnyAsync(p => EF.Functions.ILike(p.Title, title), cancellationToken)
            : await context.Project.AnyAsync(p => p.Title != null && p.Title.Equals(title, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<bool> ExistsAsync(string title, ProjectId excludeId, CancellationToken cancellationToken = default)
    {
        return SupportsILike()
            ? await context.Project.AnyAsync(p => p.Id != excludeId && EF.Functions.ILike(p.Title, title), cancellationToken)
            : await context.Project.AnyAsync(p => p.Id != excludeId && p.Title != null && p.Title.Equals(title, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Project.Add(project);
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateProjects();
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Entry(project).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateProjects();
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        context.Project.Remove(project);
        await context.SaveChangesAsync(cancellationToken);
        cacheInvalidator.InvalidateProjects();
    }

    private static IQueryable<Project> WithIncludes(IQueryable<Project> query) =>
        query
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .Include(p => p.Repositories.OrderBy(i => i.Order ?? 0));
}
