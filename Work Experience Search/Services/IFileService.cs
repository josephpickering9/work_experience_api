namespace Work_Experience_Search.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
}
