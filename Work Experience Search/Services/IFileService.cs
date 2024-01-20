namespace Work_Experience_Search.Services;

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
}
