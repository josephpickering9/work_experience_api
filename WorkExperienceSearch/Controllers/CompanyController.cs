using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Models;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Company>> GetCompany(CompanyId id)
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

    [HttpPut("{id:guid}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Company>> PutCompany(CompanyId id, [FromForm] CreateCompany createCompany)
    {
        var result = await companyService.UpdateCompanyAsync(id, createCompany);
        return result.ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteCompany(CompanyId id)
    {
        var result = await companyService.DeleteCompanyAsync(id);
        return result.ToResponse();
    }
}
