using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface ITagService
{
    Task<Result<IEnumerable<Tag>>> GetTagsAsync(string? search);
    Task<Result<Tag>> GetTagAsync(Guid id);
    Task<Result<Tag>> GetTagBySlugAsync(string slug);
    Task<Result<Tag>> CreateTagAsync(CreateTag createTag);
    Task<Result<List<Tag>>> SyncTagsAsync(List<string> tags);
    Task<Result<Tag>> UpdateTagAsync(Guid id, CreateTag updateTag);
    Task<Result<Tag>> DeleteTagAsync(Guid id);
}
