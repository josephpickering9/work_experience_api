using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface ITagService
{
    Task<Result<IEnumerable<Tag>>> GetTagsAsync(string? search);
    Task<Result<Tag>> GetTagAsync(TagId id);
    Task<Result<Tag>> GetTagBySlugAsync(string slug);
    Task<Result<Tag>> CreateTagAsync(CreateTag createTag);
    Task<Result<List<Tag>>> SyncTagsAsync(List<string> tags);
    Task<Result<Tag>> UpdateTagAsync(TagId id, CreateTag updateTag);
    Task<Result<Tag>> DeleteTagAsync(TagId id);
}
