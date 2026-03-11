using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Repositories;

public class ProjectRepository(Database context) : IProjectRepository
{
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
        return await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .Include(p => p.Repositories)
            .Include(p => p.Company)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
