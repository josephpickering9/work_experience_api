using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectImageService
{
    Task<Result<IEnumerable<ProjectImage>>> GetProjectImagesAsync(Guid projectId);
    Task<Result<ProjectImage>> GetProjectImageAsync(Guid projectId, Guid id);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(Guid projectId, List<CreateProjectImage> images);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images);
    Task<Result<bool>> OptimiseImagesAsync();
}
