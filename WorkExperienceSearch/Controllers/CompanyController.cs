using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class CompanyController(ICompanyService companyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies(string? search)
    {
        return Ok(await companyService.GetCompaniesAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        var result = await companyService.GetCompanyAsync(id);
        return result.Failure ? result.ToErrorResponse() : Ok(result.Data);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Company>> GetCompany(string slug)
    {
        var result = await companyService.GetCompanyBySlugAsync(slug);
        return result.Failure ? result.ToErrorResponse() : Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PostCompany([FromForm] CreateCompany createCompany)
    {
        var result = await companyService.CreateCompanyAsync(createCompany);
        if (result.Failure) return result.ToErrorResponse();

        return CreatedAtAction("GetCompany", new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PutCompany(int id, [FromForm] CreateCompany createCompany)
    {
        var result = await companyService.UpdateCompanyAsync(id, createCompany);
        return result.Failure ? result.ToErrorResponse() : Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var result = await companyService.DeleteCompanyAsync(id);
        return result.Failure ? result.ToErrorResponse() : NoContent();
    }
}

public class CreateCompany
{
    [Required] public string Name { get; set; } = null!;

    [Required] public string Description { get; set; } = null!;

    public IFormFile? Logo { get; set; }

    public string? Website { get; set; } = null!;
}
