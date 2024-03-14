using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services.Image;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class ProjectImageService(Database context, IFileService fileService, IImageService imageService) : IProjectImageService
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

            var imagePath = await SaveImage(image.Image);
            if (imagePath.Data == null || !imagePath.IsSuccess) return new BadRequestFailure<List<ProjectImage>>(imagePath.Error?.Message ?? "Failed to save image");

            var projectImage = new ProjectImage
            {
                Image = imagePath.Data.FileName,
                Type = image.Type,
                Project = project,
                IsOptimised = imagePath.Data.IsOptimsed
            };

            context.ProjectImage.Add(projectImage);
            await context.SaveChangesAsync();

            imagesToSave.Add(projectImage);
        }

        foreach (var image in imagesToSave)
        {
            var newImage = images.SingleOrDefault(i => i.Id == image.Id);
            if (newImage?.Order != null) image.Order = newImage.Order;
        }

        if (imagesToCreate.Count > 0 || imagesToDelete.Count > 0) await context.SaveChangesAsync();

        return new Success<List<ProjectImage>>(imagesToSave);
    }

    public async Task<Result<bool>> OptimiseImagesAsync()
    {
        var images = context.ProjectImage.Where(i => !i.IsOptimised).ToList();
        foreach (var image in images)
        {
            var file = imageService.GetImage(image.Image);
            if (file.Data == null || !file.IsSuccess) continue;

            var optimisedImage = await SaveImage(file.Data.File, image.Image);
            if (optimisedImage.Data == null || !optimisedImage.IsSuccess) continue;

            image.Image = optimisedImage.Data.FileName;
            image.IsOptimised = true;

            context.ProjectImage.Update(image);
            await context.SaveChangesAsync();

            fileService.DeleteFile(file.Data.FileName);
        }

        await context.SaveChangesAsync();
        return new Success<bool>(true);
    }

    private async Task<Result<ImageSaveData>> SaveImage(byte[] file, string fileName = "image.png")
    {
        var optimisedImage = await imageService.OptimiseImageAsync(file);
        var formFile = optimisedImage is { Data: not null, IsSuccess: true } ? FileExtensions.ByteArrayToFile(optimisedImage.Data, fileName, FileExtensions.GetContentType(fileName)) : null;
        var imageFile = await fileService.SaveFileAsync(formFile);
        var imagePath = Path.GetFileName(imageFile.Data);
        if (!imageFile.IsSuccess || imagePath == null) return new BadRequestFailure<ImageSaveData>("Image path is null or empty");

        var imageSaveData = new ImageSaveData
        {
            File = file,
            FileName = imagePath,
            IsOptimsed = optimisedImage.IsSuccess
        };

        return new Success<ImageSaveData>(imageSaveData);
    }

    private async Task<Result<ImageSaveData>> SaveImage(IFormFile? file)
    {
        var optimisedImage = await imageService.OptimiseImageAsync(await FileExtensions.FileToByteArray(file));
        var formFile = optimisedImage is { Data: not null, IsSuccess: true } ? FileExtensions.ByteArrayToFile(optimisedImage.Data, file?.FileName ?? "image.png", file?.ContentType ?? "application/octet-stream") : file;
        var imageFile = await fileService.SaveFileAsync(formFile);
        var imagePath = Path.GetFileName(imageFile.Data);
        if (!imageFile.IsSuccess || imagePath == null) return new BadRequestFailure<ImageSaveData>("Image path is null or empty");

        var imageSaveData = new ImageSaveData
        {
            File = await FileExtensions.FileToByteArray(formFile),
            FileName = imagePath,
            IsOptimsed = optimisedImage.IsSuccess
        };

        return new Success<ImageSaveData>(imageSaveData);
    }
}

public class ImageSaveData
{
    public byte[] File { get; set; }
    public string FileName { get; set; }
    public bool IsOptimsed { get; set; }
}
