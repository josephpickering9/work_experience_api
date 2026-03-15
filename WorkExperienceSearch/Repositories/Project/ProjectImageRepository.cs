using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Repositories;

public class ProjectImageRepository(Database context) : IProjectImageRepository
{
    public async Task AddAsync(ProjectImage image, CancellationToken cancellationToken = default)
    {
        context.ProjectImage.Add(image);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProjectImage image, CancellationToken cancellationToken = default)
    {
        context.ProjectImage.Update(image);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(ProjectImage image, CancellationToken cancellationToken = default)
    {
        context.ProjectImage.Remove(image);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProjectImage>> GetUnoptimisedImagesAsync(CancellationToken cancellationToken = default)
    {
        return await context.ProjectImage.Where(i => !i.IsOptimised).ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
