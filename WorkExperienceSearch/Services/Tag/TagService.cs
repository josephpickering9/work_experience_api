﻿using Microsoft.EntityFrameworkCore;
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

        if (!string.IsNullOrEmpty(search))
            tags = tags.Where(p => DatabaseExtensions.ILike(p.Title, search));

        return new Success<IEnumerable<Tag>>(await tags.ToListAsync());
    }

    public async Task<Result<Tag>> GetTagAsync(int id)
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
        var tagExists = await context.Tag.AnyAsync(p => DatabaseExtensions.ILike(p.Title, createTag.Title));
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

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

        return new Success<Tag>(tag);
    }

    public async Task<Result<List<Tag>>> SyncTagsAsync(List<string> tags)
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

        return new Success<List<Tag>>(newTags);
    }

    public async Task<Result<Tag>> UpdateTagAsync(int id, CreateTag createTag)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        var tagExists = await context.Tag.AnyAsync(t => t.Id != tag.Id && DatabaseExtensions.ILike(t.Title, createTag.Title));
        if (tagExists) return new ConflictFailure<Tag>("A tag with the same title already exists.");

        tag.Title = createTag.Title;
        tag.Type = createTag.Type;
        tag.Icon = createTag.Icon;
        tag.CustomColour = createTag.CustomColour;
        tag.Slug = createTag.Title.ToSlug();

        await context.SaveChangesAsync();

        return new Success<Tag>(tag);
    }

    public async Task<Result<Tag>> DeleteTagAsync(int id)
    {
        var tag = await context.Tag.FindAsync(id);
        if (tag == null) return new NotFoundFailure<Tag>("Tag not found.");

        context.Tag.Remove(tag);
        await context.SaveChangesAsync();

        return new Success<Tag>(tag);
    }
}
