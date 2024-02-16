using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class TagService(Database context) : ITagService
{
    public async Task<IEnumerable<Tag>> GetTagsAsync(string? search)
    {
        IQueryable<Tag> tags = context.Tag;

        if (!string.IsNullOrEmpty(search))
            tags = tags.Where(p => p.Title.ToLower().Contains(search.ToLower()));

        return await tags.ToListAsync();
    }

    public async Task<Tag> GetTagAsync(int id)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) throw new NotFoundException("Tag not found.");

        return tag;
    }

    public async Task<Tag> GetTagBySlugAsync(string slug)
    {
        var tag = await context.Tag.FirstOrDefaultAsync(t => t.Slug == slug);
        if (tag == null) throw new NotFoundException("Tag not found.");

        return tag;
    }

    public async Task<Tag> CreateTagAsync(CreateTag createTag)
    {
        var tagExists = await context.Tag
            .AnyAsync(p => p.Title.ToLower() == createTag.Title.ToLower());

        if (tagExists) throw new ConflictException("A tag with the same title already exists");

        var tag = new Tag
        {
            Title = createTag.Title,
            Type = createTag.Type,
            Icon = createTag.Icon,
            CustomColour = createTag.CustomColour,
            Slug = createTag.Title.ToSlug()
        };

        context.Tag.Add(tag);
        await context.SaveChangesAsync();

        return tag;
    }

    public async Task<List<Tag>> SyncTagsAsync(List<string> tags)
    {
        var newTags = new List<Tag>();

        foreach (var tag in tags)
        {
            var newTag =
                await context.Tag.FirstOrDefaultAsync(t =>
                    t.Title.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

            if (newTag == null)
            {
                newTag = new Tag
                {
                    Title = tag,
                    Type = TagType.Default,
                    Icon = "",
                    CustomColour = null
                };

                context.Tag.Add(newTag);
                await context.SaveChangesAsync();
            }

            newTags.Add(newTag);
        }

        return newTags;
    }

    public async Task<Tag> UpdateTagAsync(int id, CreateTag createTag)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) throw new NotFoundException("Tag not found.");

        var tagExists = await context.Tag
            .AnyAsync(p => p.Id != tag.Id && p.Title.ToLower() == createTag.Title.ToLower());

        if (tagExists) throw new ConflictException("A tag with the same title already exists");

        tag.Title = createTag.Title;
        tag.Type = createTag.Type;
        tag.Icon = createTag.Icon;
        tag.CustomColour = createTag.CustomColour;
        tag.Slug = createTag.Title.ToSlug();

        await context.SaveChangesAsync();

        return tag;
    }


    public async Task<Tag> DeleteTagAsync(int id)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) throw new NotFoundException("Tag not found.");

        context.Tag.Remove(tag);
        await context.SaveChangesAsync();

        return tag;
    }
}
