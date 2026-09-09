using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services.Image;
using Work_Experience_Search.Types;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class CompanyService(ICompanyRepository repository, IFileService fileService, IImageService imageService, ILogger<CompanyService> logger) : ICompanyService
{
    public async Task<Result<IEnumerable<Company>>> GetCompaniesAsync(string? search)
    {
        var companies = await repository.SearchAsync(search);
        return new Success<IEnumerable<Company>>(companies);
    }

    public async Task<Result<Company>> GetCompanyAsync(CompanyId id)
    {
        var company = await repository.GetAsync(id);
        if (company == null)
        {
            logger.LogWarning("Company {CompanyId} not found.", id);
            return new NotFoundFailure<Company>("Company not found.");
        }

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> GetCompanyBySlugAsync(string slug)
    {
        var company = await repository.GetAsync(slug);
        if (company == null)
        {
            logger.LogWarning("Company with slug {Slug} not found.", slug);
            return new NotFoundFailure<Company>("Company not found.");
        }

        return new Success<Company>(company);
    }

    public async Task<Result<Company>> CreateCompanyAsync(CreateCompany createCompany)
    {
        var companyExists = await repository.ExistsAsync(createCompany.Name);
        if (companyExists)
        {
            logger.LogWarning("Company creation conflict: a company named {Name} already exists.", createCompany.Name);
            return new ConflictFailure<Company>("A company with the same title already exists.");
        }

        string? logoPath = null;
        if (createCompany.Logo is { Length: > 0 })
        {
            var logoFile = await SaveLogoAsync(createCompany.Logo);
            if (!logoFile.IsSuccess)
            {
                logger.LogWarning("Logo could not be saved while creating company {Name}: {Reason}", createCompany.Name, logoFile.Error?.Message);
                return new BadRequestFailure<Company>("Logo file could not be saved.");
            }

            logoPath = logoFile.Data;
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
        if (company == null)
        {
            logger.LogWarning("Company {CompanyId} not found for update.", id);
            return new NotFoundFailure<Company>("Company not found.");
        }

        var companyExists = await repository.ExistsAsync(createCompany.Name, id);
        if (companyExists)
        {
            logger.LogWarning("Company {CompanyId} update conflict: a company named {Name} already exists.", id, createCompany.Name);
            return new ConflictFailure<Company>("A company with the same title already exists.");
        }

        string? logoPath = null;
        if (createCompany.Logo is { Length: > 0 })
        {
            var logoFile = await SaveLogoAsync(createCompany.Logo);
            if (!logoFile.IsSuccess)
            {
                logger.LogWarning("Logo could not be saved while updating company {CompanyId}: {Reason}", id, logoFile.Error?.Message);
                return new BadRequestFailure<Company>("Logo file could not be saved.");
            }

            logoPath = logoFile.Data;
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
        if (company == null)
        {
            logger.LogWarning("Company {CompanyId} not found for deletion.", id);
            return new NotFoundFailure<Company>("Company not found.");
        }

        await repository.DeleteAsync(company);

        return new Success<Company>(company);
    }

    private async Task<Result<string>> SaveLogoAsync(IFormFile file)
    {
        var optimisedImage = await imageService.OptimiseImageAsync(await FileExtensions.FileToByteArray(file));
        var formFile = optimisedImage is { Data: not null, IsSuccess: true }
            ? FileExtensions.ByteArrayToFile(optimisedImage.Data, file.FileName, file.ContentType)
            : file;
        var savedFile = await fileService.SaveFileAsync(formFile);
        if (!savedFile.IsSuccess) return new BadRequestFailure<string>("Logo file could not be saved.");

        var fileName = Path.GetFileName(savedFile.Data);
        if (fileName == null) return new BadRequestFailure<string>("Logo file path is null or empty.");

        return new Success<string>(fileName);
    }
}
