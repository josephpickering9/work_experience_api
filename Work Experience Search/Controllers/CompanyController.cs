using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _tagService;

    public CompanyController(ICompanyService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies(string? search)
    {
        return Ok(await _tagService.GetCompaniesAsync(search));
    }

    [HttpGet("id")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        try
        {
            return await _tagService.GetCompanyAsync(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PostCompany([FromForm] CreateCompany createCompany)
    {
        try
        {
            var tag = await _tagService.CreateCompanyAsync(createCompany);
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

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PutCompany(int id, [FromForm] CreateCompany createCompany)
    {
        try
        {
            var tag = await _tagService.UpdateCompanyAsync(id, createCompany);
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

    [HttpDelete("id")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        try
        {
            await _tagService.DeleteCompanyAsync(id);
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
    [Required] public string Name { get; set; }

    [Required] public string Description { get; set; }

    public IFormFile? Logo { get; set; }

    public string? Website { get; set; } = null!;
}
