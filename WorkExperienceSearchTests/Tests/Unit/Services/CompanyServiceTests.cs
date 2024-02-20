using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Utils;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit.Services;

public class CompanyServiceTests : BaseServiceTests, IAsyncLifetime
{
    private readonly CompanyService _companyService;

    public CompanyServiceTests()
    {
        var mockFileService = new Mock<IFileService>();
        _companyService = new CompanyService(Context, mockFileService.Object);
    }

    public async Task InitializeAsync()
    {
        await ClearDatabase();
        await SeedDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
        const int companyId = 1;

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
        const int companyId = 99;

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
            Website = "https://example.com"
        };

        // Act
        var result = (await _companyService.CreateCompanyAsync(createCompany)).ExpectSuccess();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal(createCompany.Name, result.Name);
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
        const int companyId = 1;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
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
    }

    [Fact]
    public async Task UpdateCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int companyId = 99;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
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
        const int companyId = 1;

        // Act
        (await _companyService.DeleteCompanyAsync(companyId)).ExpectSuccess();

        // Assert
        var companyInDb = await Context.Company.FindAsync(companyId);
        Assert.Null(companyInDb);
    }

    [Fact]
    public async Task DeleteCompanyAsync_InvalidId_ThrowsNotFoundFailure()
    {
        // Arrange
        const int companyId = 99;

        // Act
        var result = (await _companyService.DeleteCompanyAsync(companyId)).ExpectFailure();

        // Assert
        Assert.IsType<NotFoundException>(result);
        Assert.Equal("Company not found.", result.Message);
    }

    private async Task SeedDatabase()
    {
        if (!Context.Company.Any())
        {
            Context.Company.AddRange(GetTestCompanies());
            await Context.SaveChangesAsync();
        }
    }

    private static IEnumerable<Company> GetTestCompanies()
    {
        var company = CreateCompany(1, "Test Company", "Test Description", "testLogo", "https://example.com");

        return [company];
    }
}