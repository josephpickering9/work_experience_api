using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetTagsAsync(string? search);
    Task<Tag> GetTagAsync(int id);
    Task<Tag> GetTagBySlugAsync(string slug);
    Task<Tag> CreateTagAsync(CreateTag createTag);
    Task<List<Tag>> SyncTagsAsync(List<string> tags);
    Task<Tag> UpdateTagAsync(int id, CreateTag updateTag);
    Task<Tag> DeleteTagAsync(int id);
}
