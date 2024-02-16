using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class CompanyController(ICompanyService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies(string? search)
    {
        return Ok(await tagService.GetCompaniesAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        try
        {
            return await tagService.GetCompanyAsync(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Company>> GetCompany(string slug)
    {
        try
        {
            return await tagService.GetCompanyBySlugAsync(slug);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PostCompany([FromForm] CreateCompany createCompany)
    {
        try
        {
            var tag = await tagService.CreateCompanyAsync(createCompany);
            return CreatedAtAction("GetCompany", new { id = tag.Id }, tag);
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

    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PutCompany(int id, [FromForm] CreateCompany createCompany)
    {
        try
        {
            var tag = await tagService.UpdateCompanyAsync(id, createCompany);
            return CreatedAtAction("GetCompany", new { id = tag.Id }, tag);
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

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        try
        {
            await tagService.DeleteCompanyAsync(id);
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

public class CreateCompany
{
    [Required] public string Name { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public IFormFile? Logo { get; set; }

    public string? Website { get; set; } = null!;
}
