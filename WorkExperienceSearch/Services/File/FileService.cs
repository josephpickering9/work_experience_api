namespace Work_Experience_Search.Services;

public class FileService(IWebHostEnvironment hostEnvironment) : IFileService
{
    public async Task<string?> SaveFileAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "";

        var uploadDir = Path.Combine(hostEnvironment.WebRootPath ?? hostEnvironment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadDir, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        return filePath;
    }

    public void DeleteFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
