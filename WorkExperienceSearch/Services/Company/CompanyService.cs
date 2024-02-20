using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class CompanyService(Database context, IFileService fileService) : ICompanyService
{
    public async Task<IEnumerable<Company>> GetCompaniesAsync(string? search)
    {
        IQueryable<Company> companies = context.Company;

        if (!string.IsNullOrEmpty(search))
            companies = companies.Where(c => DatabaseExtensions.ILike(c.Name, search));

        return await companies.ToListAsync();
    }

    public async Task<Result<Company>> GetCompanyAsync(int id)
    {
        var company = await context.Company.FindAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> GetCompanyBySlugAsync(string slug)
    {
        var company = await context.Company.FirstOrDefaultAsync(p => p.Slug == slug);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> CreateCompanyAsync(CreateCompany createCompany)
    {
        var companyExists = await context.Company.AnyAsync(c => DatabaseExtensions.ILike(c.Name, createCompany.Name));
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
            Slug = createCompany.Name.ToSlug()
        };

        context.Company.Add(company);
        await context.SaveChangesAsync();

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> UpdateCompanyAsync(int id, CreateCompany createCompany)
    {
        var company = await context.Company.FindAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        var companyExists = await context.Company.AnyAsync(p =>
            p.Id != company.Id && DatabaseExtensions.ILike(p.Name, createCompany.Name));
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
        company.Slug = createCompany.Name.ToSlug();

        await context.SaveChangesAsync();

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> DeleteCompanyAsync(int id)
    {
        var company = await context.Company.FindAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        context.Company.Remove(company);
        await context.SaveChangesAsync();

        return new Success<Company>(company);
    }
}
