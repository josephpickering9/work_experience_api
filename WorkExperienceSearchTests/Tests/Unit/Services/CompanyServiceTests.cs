using Microsoft.Extensions.Caching.Memory;
using Moq;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Services;
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
        _companyService = new CompanyService(new CompanyRepository(Context, new MemoryCache(new MemoryCacheOptions()), new CacheInvalidator()), mockFileService.Object);
    }
    
    [Fact]
    public async Task GetCompaniesAsync_NoSearch_ReturnsAllCompanies()
    {
        // Act
        var result = (await _companyService.GetCompaniesAsync(null)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetCompanyAsync_ValidId_ReturnsCompany()
    {
        // Arrange
        var companyId = Company1Id;

        // Act
        var result = (await _companyService.GetCompanyAsync(companyId)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);
    }

    [Fact]
    public async Task GetCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        var companyId = CompanyId.New();

        // Act
        var result = (await _companyService.GetCompanyAsync(companyId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAsync_NewCompany_ReturnsCompany()
    {
        // Arrange
        var createCompany = new CreateCompany
        {
            Name = "New Company",
            Description = "New Description",
            Logo = null,
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://example.com"
        };

        // Act
        var result = (await _companyService.CreateCompanyAsync(createCompany)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createCompany.Name, result.Name);
        Assert.Equal(createCompany.StartDate, result.StartDate);
        Assert.Equal(createCompany.EndDate, result.EndDate);
    }

    [Fact]
    public async Task CreateCompanyAsync_ExistingCompany_ThrowsConflictFailure()
    {
        // Arrange
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

        // Act
        var result = (await _companyService.CreateCompanyAsync(createCompany)).ExpectFailure();

        // Assert
        Assert.IsType<ConflictException>(result);
        Assert.Equal("A company with the same title already exists.", result.Message);
    }

    [Fact]
    public async Task UpdateCompanyAsync_ValidId_ReturnsUpdatedCompany()
    {
        // Arrange
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

        // Act
        var result = (await _companyService.UpdateCompanyAsync(companyId, updateCompany)).ExpectSuccess();

        // Assert
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
        // Arrange
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

        // Act
        var result = (await _companyService.UpdateCompanyAsync(companyId, updateCompany)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }

    [Fact]
    public async Task DeleteCompanyAsync_ValidId_DeletesCompany()
    {
        // Arrange
        var companyId = Company1Id;

        // Act
        (await _companyService.DeleteCompanyAsync(Company1Id)).ExpectSuccess();

        // Assert
        var companyInDb = await Context.Company.FindAsync(companyId);
        Assert.Null(companyInDb);
    }

    [Fact]
    public async Task DeleteCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        var companyId = CompanyId.New();

        // Act
        var result = (await _companyService.DeleteCompanyAsync(companyId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }
}
