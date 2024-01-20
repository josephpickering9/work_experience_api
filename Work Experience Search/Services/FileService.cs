namespace Work_Experience_Search.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _hostEnvironment;

    public FileService(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }
    
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }

        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        string filePath = Path.Combine(uploadDir, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return filePath;
    }
}