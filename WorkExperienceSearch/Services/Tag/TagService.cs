using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class TagService(ITagRepository repository) : ITagService
{
    public async Task<Result<IEnumerable<Tag>>> GetTagsAsync(string? search)
    {
        var tags = await repository.SearchAsync(search);
        return new Success<IEnumerable<Tag>>(tags);
    }

    public async Task<Result<Tag>> GetTagAsync(TagId id)
    {
        var tag = await repository.GetAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> GetTagBySlugAsync(string slug)
    {
        var tag = await repository.GetAsync(slug);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> CreateTagAsync(CreateTag createTag)
    {
        var tagExists = await repository.ExistsAsync(createTag.Title);
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

        var tag = new Tag
        {
            Title = createTag.Title,
            Type = createTag.Type,
            Icon = createTag.Icon,
            CustomColour = createTag.CustomColour,
            Slug = createTag.Title.ToSlug()
        };

        await repository.AddAsync(tag);

        return new Success<Tag>(tag);
    }

    public async Task<Result<List<Tag>>> SyncTagsAsync(List<string> tags)
    {
        var tagsList = new List<Tag>();

        foreach (var tag in tags)
        {
            var existingTag = await repository.GetByTitleAsync(tag);
            if (existingTag != null)
            {
                tagsList.Add(existingTag);
            }
            else
            {
                var newTag = new Tag
                {
                    Title = tag,
                    Type = TagType.Default,
                    Icon = "",
                    CustomColour = null
                };

                await repository.AddAsync(newTag);

                tagsList.Add(newTag);
            }
        }

        return new Success<List<Tag>>(tagsList);
    }

    public async Task<Result<Tag>> UpdateTagAsync(TagId id, CreateTag createTag)
    {
        var tag = await repository.GetAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        var tagExists = await repository.ExistsAsync(createTag.Title, id);
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

        tag.Title = createTag.Title;
        tag.Type = createTag.Type;
        tag.Icon = createTag.Icon;
        tag.CustomColour = createTag.CustomColour;
        tag.Slug = createTag.Title.ToSlug();

        await repository.UpdateAsync(tag);

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> DeleteTagAsync(TagId id)
    {
        var tag = await repository.GetAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        await repository.DeleteAsync(tag);

        return new Success<Tag>(tag);
    }
}
