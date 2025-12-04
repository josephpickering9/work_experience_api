using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class TagService(Database context) : ITagService
{
    public async Task<Result<IEnumerable<Tag>>> GetTagsAsync(string? search)
    {
        IQueryable<Tag> tags = context.Tag;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            tags = SupportsILike()
                ? tags.Where(p => EF.Functions.ILike(p.Title, $"%{search}%"))
                : tags.Where(p => p.Title != null && p.Title.ToLower().Contains(normalizedSearch));
        }

        return new Success<IEnumerable<Tag>>(await tags.ToListAsync());
    }

    public async Task<Result<Tag>> GetTagAsync(Guid id)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> GetTagBySlugAsync(string slug)
    {
        var tag = await context.Tag.FirstOrDefaultAsync(t => t.Slug == slug);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> CreateTagAsync(CreateTag createTag)
    {
        var tagExists = SupportsILike()
            ? await context.Tag.AnyAsync(p => EF.Functions.ILike(p.Title, createTag.Title))
            : await context.Tag.AnyAsync(p => p.Title != null && p.Title.Equals(createTag.Title, StringComparison.OrdinalIgnoreCase));
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

        var tag = new Tag
        {
            Title = createTag.Title,
            Type = createTag.Type,
            Icon = createTag.Icon,
            CustomColour = createTag.CustomColour,
            Slug = createTag.Title.ToSlug()
        };

        var test = context.Tag.ToList();

        context.Tag.Add(tag);
        await context.SaveChangesAsync();

        return new Success<Tag>(tag);
    }

    public async Task<Result<List<Tag>>> SyncTagsAsync(List<string> tags)
    {
        var tagsList = new List<Tag>();

        foreach (var tag in tags)
        {
            var existingTag = SupportsILike()
                ? await context.Tag.FirstOrDefaultAsync(t => EF.Functions.ILike(tag, t.Title))
                : await context.Tag.FirstOrDefaultAsync(t => t.Title != null && t.Title.Equals(tag, StringComparison.OrdinalIgnoreCase));
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

                context.Tag.Add(newTag);
                await context.SaveChangesAsync();

                tagsList.Add(newTag);
            }
        }

        return new Success<List<Tag>>(tagsList);
    }

    public async Task<Result<Tag>> UpdateTagAsync(Guid id, CreateTag createTag)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        var tagExists = SupportsILike()
            ? await context.Tag.AnyAsync(t => t.Id != tag.Id && EF.Functions.ILike(t.Title, createTag.Title))
            : await context.Tag.AnyAsync(t => t.Id != tag.Id && t.Title != null && t.Title.Equals(createTag.Title, StringComparison.OrdinalIgnoreCase));
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

        tag.Title = createTag.Title;
        tag.Type = createTag.Type;
        tag.Icon = createTag.Icon;
        tag.CustomColour = createTag.CustomColour;
        tag.Slug = createTag.Title.ToSlug();

        await context.SaveChangesAsync();

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> DeleteTagAsync(Guid id)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        context.Tag.Remove(tag);
        await context.SaveChangesAsync();

        return new Success<Tag>(tag);
    }

    private bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
