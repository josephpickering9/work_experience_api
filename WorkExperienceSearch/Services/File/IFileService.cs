using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface IFileService
{
    Task<Result<string>> SaveFileAsync(byte[] file, string name = "image.png");
    Task<Result<string>> SaveFileAsync(IFormFile? file);
    void DeleteFile(string? filePath);
}
