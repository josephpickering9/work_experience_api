using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectImageService
{
    Task<Result<IEnumerable<ProjectImage>>> GetProjectImagesAsync(int projectId);
    Task<Result<ProjectImage>> GetProjectImageAsync(int projectId, int id);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(int projectId, List<CreateProjectImage> images);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images);
}
