using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class CompanyService(ICompanyRepository repository, IFileService fileService) : ICompanyService
{
    public async Task<Result<IEnumerable<Company>>> GetCompaniesAsync(string? search)
    {
        var companies = await repository.SearchAsync(search);
        return new Success<IEnumerable<Company>>(companies);
    }

    public async Task<Result<Company>> GetCompanyAsync(CompanyId id)
    {
        var company = await repository.GetAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> GetCompanyBySlugAsync(string slug)
    {
        var company = await repository.GetAsync(slug);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> CreateCompanyAsync(CreateCompany createCompany)
    {
        var companyExists = await repository.ExistsAsync(createCompany.Name);
        if (companyExists) return new ConflictFailure<Company>("A company with the same title already exists.");

        string? logoPath = null;
        if (createCompany.Logo != null)
        {
            var logoFile = await fileService.SaveFileAsync(createCompany.Logo);
            if (!logoFile.IsSuccess) return new BadRequestFailure<Company>("Logo file could not be saved.");

            logoPath = Path.GetFileName(logoFile.Data);
        }

        var company = new Company
        {
            Name = createCompany.Name,
            Description = createCompany.Description,
            Logo = logoPath,
            Website = createCompany.Website,
            StartDate = createCompany.StartDate,
            EndDate = createCompany.EndDate,
            Slug = createCompany.Name.ToSlug()
        };

        await repository.AddAsync(company);

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> UpdateCompanyAsync(CompanyId id, CreateCompany createCompany)
    {
        var company = await repository.GetAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        var companyExists = await repository.ExistsAsync(createCompany.Name, id);
        if (companyExists) return new ConflictFailure<Company>("A company with the same title already exists.");

        string? logoPath = null;
        if (createCompany.Logo != null)
        {
            var logoFile = await fileService.SaveFileAsync(createCompany.Logo);
            if (!logoFile.IsSuccess) return new BadRequestFailure<Company>("Logo file could not be saved.");

            logoPath = Path.GetFileName(logoFile.Data);
        }

        if (logoPath != null) company.Logo = logoPath;

        company.Name = createCompany.Name;
        company.Description = createCompany.Description;
        company.Website = createCompany.Website;
        company.StartDate = createCompany.StartDate;
        company.EndDate = createCompany.EndDate;
        company.Slug = createCompany.Name.ToSlug();

        await repository.UpdateAsync(company);

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> DeleteCompanyAsync(CompanyId id)
    {
        var company = await repository.GetAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        await repository.DeleteAsync(company);

        return new Success<Company>(company);
    }
}
