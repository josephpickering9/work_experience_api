using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IProjectImageService
{
    Task<Result<IEnumerable<ProjectImage>>> GetProjectImagesAsync(ProjectId projectId);
    Task<Result<ProjectImage>> GetProjectImageAsync(ProjectId projectId, ProjectImageId id);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(ProjectId projectId, List<CreateProjectImage> images);
    Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images);
    Task<Result<bool>> OptimiseImagesAsync();
}
