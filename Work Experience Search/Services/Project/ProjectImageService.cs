using Microsoft.IdentityModel.Tokens;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class ProjectImageService : IProjectImageService
{
    private readonly Database _context;
    private readonly IFileService _fileService;

    public ProjectImageService(Database context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<IEnumerable<ProjectImage>> GetProjectImagesAsync(int projectId)
    {
        var project = await _context.Project.FindAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found.");

        return project.Images;
    }

    public async Task<ProjectImage> GetProjectImageAsync(int projectId, int id)
    {
        var project = await _context.Project.FindAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found.");

        var image = project.Images.SingleOrDefault(i => i.Id == id);
        if (image == null) throw new NotFoundException("Image not found.");

        return image;
    }

    public async Task<List<ProjectImage>> SyncProjectImagesAsync(int projectId, List<CreateProjectImage> images)
    {
        var project = await _context.Project.FindAsync(projectId);
        if (project == null) throw new NotFoundException("Project not found.");

        return await SyncProjectImagesAsync(project, images);
    }

    public async Task<List<ProjectImage>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images)
    {
        var imageIds = images.Select(i => i.Id).ToList();
        var imagesToDelete = project.Images.Where(i => !imageIds.Contains(i.Id)).ToList();
        var imagesToCreate = images.Where(i => i.Id == null).ToList();
        var imagesToSave = project.Images.Where(i => images.Any(x => x.Id == i.Id)).ToList();

        var projectImages = imagesToSave;

        foreach (var image in imagesToDelete)
        {
            _context.ProjectImage.Remove(image);
            _fileService.DeleteFile(image.Image);
        }

        var singleTypes = new List<ImageType> { ImageType.Logo, ImageType.Banner, ImageType.Card };
        foreach (var image in imagesToCreate)
        {
            if (singleTypes.Contains(image.Type))
            {
                var existingImage = project.Images.SingleOrDefault(i => i.Type == image.Type);
                if (existingImage != null)
                {
                    _fileService.DeleteFile(existingImage.Image);
                    _context.ProjectImage.Remove(existingImage);
                }
            }

            var imagePath = Path.GetFileName(await _fileService.SaveFileAsync(image.Image));
            if (imagePath.IsNullOrEmpty()) throw new InvalidOperationException("Image could not be saved.");

            var projectImage = new ProjectImage
            {
                Image = imagePath,
                Type = image.Type
            };

            _context.ProjectImage.Add(projectImage);
            await _context.SaveChangesAsync();

            projectImages.Add(projectImage);
        }

        if (imagesToCreate.Count > 0 || imagesToDelete.Count > 0) await _context.SaveChangesAsync();

        return projectImages;
    }
}
