using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public interface ICompanyService
{
    Task<Result<IEnumerable<Company>>> GetCompaniesAsync(string? search);
    Task<Result<Company>> GetCompanyAsync(Guid id);
    Task<Result<Company>> GetCompanyBySlugAsync(string slug);
    Task<Result<Company>> CreateCompanyAsync(CreateCompany createCompany);
    Task<Result<Company>> UpdateCompanyAsync(Guid id, CreateCompany updateCompany);
    Task<Result<Company>> DeleteCompanyAsync(Guid id);
}
