using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var result = await companyService.GetCompaniesAsync(search);
        return result.ToResponse();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        var result = await companyService.GetCompanyAsync(id);
        return result.ToResponse();
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Company>> GetCompany(string slug)
    {
        var result = await companyService.GetCompanyBySlugAsync(slug);
        return result.ToResponse();
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PostCompany([FromForm] CreateCompany createCompany)
    {
        var result = await companyService.CreateCompanyAsync(createCompany);
        return result.ToResponse();
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PutCompany(int id, [FromForm] CreateCompany createCompany)
    {
        var result = await companyService.UpdateCompanyAsync(id, createCompany);
        return result.ToResponse();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var result = await companyService.DeleteCompanyAsync(id);
        return result.ToResponse();
    }
}

public class CreateCompany
{
    [Required] public string Name { get; init; } = null!;

    [Required] public string Description { get; init; } = null!;

    public IFormFile? Logo { get; init; }

    public string? Website { get; init; }
}
