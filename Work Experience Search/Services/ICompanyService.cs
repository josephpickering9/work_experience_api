using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public interface ICompanyService
{
    Task<IEnumerable<Company>> GetCompaniesAsync(string? search);
    Task<Company> GetCompanyAsync(int id);
    Task<Company> CreateCompanyAsync(CreateCompany createCompany);
    Task<Company> UpdateCompanyAsync(int id, CreateCompany updateCompany);
    Task<Company> DeleteCompanyAsync(int id);
}
