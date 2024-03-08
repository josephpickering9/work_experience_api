using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public class ProjectImageService(Database context, IFileService fileService) : IProjectImageService
{
    public async Task<Result<IEnumerable<ProjectImage>>> GetProjectImagesAsync(int projectId)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<IEnumerable<ProjectImage>>("Project not found.");

        return new Success<IEnumerable<ProjectImage>>(project.Images);
    }

    public async Task<Result<ProjectImage>> GetProjectImageAsync(int projectId, int id)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<ProjectImage>("Project not found.");

        var image = project.Images.SingleOrDefault(i => i.Id == id);
        if (image == null) return new NotFoundFailure<ProjectImage>("Image not found.");

        return new Success<ProjectImage>(image);
    }

    public async Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(int projectId, List<CreateProjectImage> images)
    {
        var project = await context.Project.FindAsync(projectId);
        if (project == null) return new NotFoundFailure<List<ProjectImage>>("Project not found.");

        return await SyncProjectImagesAsync(project, images);
    }

    public async Task<Result<List<ProjectImage>>> SyncProjectImagesAsync(Project project, List<CreateProjectImage> images)
    {
        var imageIds = images.Select(i => i.Id).ToList();
        var imagesToDelete = project.Images.Where(i => !imageIds.Contains(i.Id)).ToList();
        var imagesToCreate = images.Where(i => i.Id == null).ToList();
        var imagesToSave = project.Images.Where(i => images.Any(x => x.Id == i.Id)).ToList();

        foreach (var image in imagesToDelete)
        {
            context.ProjectImage.Remove(image);
            fileService.DeleteFile(image.Image);
        }

        var singleTypes = new List<ImageType> { ImageType.Logo, ImageType.Banner, ImageType.Card };
        foreach (var image in imagesToCreate)
        {
            if (singleTypes.Contains(image.Type))
            {
                var existingImage = project.Images.SingleOrDefault(i => i.Type == image.Type);
                if (existingImage != null)
                {
                    fileService.DeleteFile(existingImage.Image);
                    context.ProjectImage.Remove(existingImage);
                }
            }

            var imageFile = await fileService.SaveFileAsync(image.Image);
            var imagePath = Path.GetFileName(imageFile.Data);
            if (!imageFile.IsSuccess || imagePath == null) return new BadRequestFailure<List<ProjectImage>>("Image path is null or empty");

            var projectImage = new ProjectImage
            {
                Image = imagePath,
                Type = image.Type,
                Project = project
            };

            context.ProjectImage.Add(projectImage);
            await context.SaveChangesAsync();

            imagesToSave.Add(projectImage);
        }

        foreach (var image in imagesToSave)
        {
            var newImage = images.Single(i => i.Id == image.Id);
            image.Order = newImage.Order;
        }

        if (imagesToCreate.Count > 0 || imagesToDelete.Count > 0) await context.SaveChangesAsync();

        return new Success<List<ProjectImage>>(imagesToSave);
    }
}
