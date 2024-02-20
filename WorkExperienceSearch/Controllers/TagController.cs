using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class TagController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tag>>> GetTags(string? search)
    {
        var tags = await tagService.GetTagsAsync(search);
        return tags.ToResponse();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tag>> GetTag(int id)
    {
        var result = await tagService.GetTagAsync(id);
        return result.ToResponse();
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Tag>> GetTag(string slug)
    {
        var result = await tagService.GetTagBySlugAsync(slug);
        return result.ToResponse();
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Tag>> PostTag([FromBody] CreateTag createTag)
    {
        var result = await tagService.CreateTagAsync(createTag);
        return result.ToResponse();
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<Tag>> PutTag(int id, [FromBody] CreateTag createTag)
    {
        var result = await tagService.UpdateTagAsync(id, createTag);
        return result.ToResponse();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await tagService.DeleteTagAsync(id);
        return result.ToResponse();
    }
}

public class CreateTag
{
    [Required] public string Title { get; init; } = null!;

    [Required] public TagType Type { get; init; }

    public string? Icon { get; init; }

    public string? CustomColour { get; init; }
}
