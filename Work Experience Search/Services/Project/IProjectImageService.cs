using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public interface IProjectImageService
{
    Task<IEnumerable<ProjectImage>> GetProjectImagesAsync(int projectId);
    Task<ProjectImage> GetProjectImageAsync(int projectId, int id);
    Task<List<ProjectImage>> SyncProjectImagesAsync(int projectId, List<CreateProjectImage> images);
    Task<List<ProjectImage>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images);
}
