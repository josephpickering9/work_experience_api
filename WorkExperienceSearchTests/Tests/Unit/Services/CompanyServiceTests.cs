using Microsoft.Extensions.Caching.Memory;
using Moq;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Services;
using Work_Experience_Search.Services.Image;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class CompanyServiceTests : BaseServiceTests
{
    private readonly CompanyService _companyService;

    public CompanyServiceTests()
    {
        var mockFileService = new Mock<IFileService>();
        var mockImageService = new Mock<IImageService>();
        _companyService = new CompanyService(new CompanyRepository(Context, new MemoryCache(new MemoryCacheOptions()), new CacheInvalidator()), mockFileService.Object, mockImageService.Object);
    }
    
    [Fact]
    public async Task GetCompaniesAsync_NoSearch_ReturnsAllCompanies()
    {
        var result = (await _companyService.GetCompaniesAsync(null)).ExpectSuccess();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetCompanyAsync_ValidId_ReturnsCompany()
    {
        var companyId = Company1Id;

        var result = (await _companyService.GetCompanyAsync(companyId)).ExpectSuccess();

        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);
    }

    [Fact]
    public async Task GetCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        var companyId = CompanyId.New();

        var result = (await _companyService.GetCompanyAsync(companyId)).ExpectFailure();

        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAsync_NewCompany_ReturnsCompany()
    {
        var createCompany = new CreateCompany
        {
            Name = "New Company",
            Description = "New Description",
            Logo = null,
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://example.com"
        };

        var result = (await _companyService.CreateCompanyAsync(createCompany)).ExpectSuccess();

        Assert.NotNull(result);
        Assert.Equal(createCompany.Name, result.Name);
        Assert.Equal(createCompany.StartDate, result.StartDate);
        Assert.Equal(createCompany.EndDate, result.EndDate);
    }

    [Fact]
    public async Task CreateCompanyAsync_ExistingCompany_ThrowsConflictFailure()
    {
        var createCompany = new CreateCompany
        {
            Name = "Conflict Company",
            Description = "Conflict Description",
            Logo = null,
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://example.com"
        };
        (await _companyService.CreateCompanyAsync(createCompany)).ExpectSuccess();

        var result = (await _companyService.CreateCompanyAsync(createCompany)).ExpectFailure();

        Assert.IsType<ConflictException>(result);
        Assert.Equal("A company with the same title already exists.", result.Message);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ValidId_ReturnsUpdatedCompany()
    {
        var companyId = Company1Id;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://updated-example.com"
        };

        var result = (await _companyService.UpdateCompanyAsync(companyId, updateCompany)).ExpectSuccess();

        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);
        Assert.Equal(updateCompany.Name, result.Name);
        Assert.Equal(updateCompany.Description, result.Description);
        Assert.Equal(updateCompany.Website, result.Website);
        Assert.Equal(updateCompany.StartDate, result.StartDate);
        Assert.Equal(updateCompany.EndDate, result.EndDate);
    }

    [Fact]
    public async Task UpdateCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        var companyId = CompanyId.New();
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
            StartDate = new DateOnly(2021, 1, 1),
            EndDate = new DateOnly(2022, 1, 1),
            Website = "https://updated-example.com"
        };

        var result = (await _companyService.UpdateCompanyAsync(companyId, updateCompany)).ExpectFailure();

        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }

    [Fact]
    public async Task DeleteCompanyAsync_ValidId_DeletesCompany()
    {
        var companyId = Company1Id;

        (await _companyService.DeleteCompanyAsync(Company1Id)).ExpectSuccess();

        var companyInDb = await Context.Company.FindAsync(companyId);
        Assert.Null(companyInDb);
    }

    [Fact]
    public async Task DeleteCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        var companyId = CompanyId.New();

        var result = (await _companyService.DeleteCompanyAsync(companyId)).ExpectFailure();

        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }
}
