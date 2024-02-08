namespace Work_Experience_Search.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _hostEnvironment;

    public FileService(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public async Task<string?> SaveFileAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "";

        var uploadDir = Path.Combine(_hostEnvironment.WebRootPath ?? _hostEnvironment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadDir, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return filePath;
    }

    public void DeleteFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
