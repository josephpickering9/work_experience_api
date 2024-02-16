using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
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
        return Ok(await tagService.GetTagsAsync(search));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Tag>> GetTag(int id)
    {
        try
        {
            return await tagService.GetTagAsync(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Tag>> GetTag(string slug)
    {
        try
        {
            return await tagService.GetTagBySlugAsync(slug);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Tag>> PostTag([FromBody] CreateTag createTag)
    {
        try
        {
            var tag = await tagService.CreateTagAsync(createTag);
            return CreatedAtAction("GetTag", new { id = tag.Id }, tag);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<Tag>> PutTag(int id, [FromBody] CreateTag createTag)
    {
        try
        {
            var tag = await tagService.UpdateTagAsync(id, createTag);
            return CreatedAtAction("GetTag", new { id = tag.Id }, tag);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTag(int id)
    {
        try
        {
            await tagService.DeleteTagAsync(id);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}

public class CreateTag
{
    [Required] public string Title { get; set; } = null!;

    [Required] public TagType Type { get; set; }

    public string? Icon { get; set; }

    public string? CustomColour { get; set; }
}
