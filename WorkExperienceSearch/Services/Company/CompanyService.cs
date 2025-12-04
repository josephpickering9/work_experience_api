using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class CompanyService(Database context, IFileService fileService) : ICompanyService
{
    public async Task<Result<IEnumerable<Company>>> GetCompaniesAsync(string? search)
    {
        IQueryable<Company> companies = context.Company;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLowerInvariant();
            companies = SupportsILike()
                ? companies.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"))
                : companies.Where(c => c.Name != null && c.Name.ToLower().Contains(normalizedSearch));
        }

        return new Success<IEnumerable<Company>>(await companies.ToListAsync());
    }

    public async Task<Result<Company>> GetCompanyAsync(Guid id)
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
        var companyExists = SupportsILike()
            ? await context.Company.AnyAsync(c => EF.Functions.ILike(c.Name, createCompany.Name))
            : await context.Company.AnyAsync(c => c.Name != null && c.Name.Equals(createCompany.Name, StringComparison.OrdinalIgnoreCase));
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

    public async Task<Result<Company>> UpdateCompanyAsync(Guid id, CreateCompany createCompany)
    {
        var company = await context.Company.FindAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        var companyExists = SupportsILike()
            ? await context.Company.AnyAsync(p => p.Id != company.Id && EF.Functions.ILike(p.Name, createCompany.Name))
            : await context.Company.AnyAsync(p => p.Id != company.Id && p.Name != null && p.Name.Equals(createCompany.Name, StringComparison.OrdinalIgnoreCase));
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

    public async Task<Result<Company>> DeleteCompanyAsync(Guid id)
    {
        var company = await context.Company.FindAsync(id);
        if (company == null) return new NotFoundFailure<Company>("Company not found.");

        context.Company.Remove(company);
        await context.SaveChangesAsync();

        return new Success<Company>(company);
    }

    private bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
