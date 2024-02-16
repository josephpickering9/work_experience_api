using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Tests.Unit;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Unit;

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
        var result = await _companyService.GetCompaniesAsync(null);

        // Assert
        Assert.Equal(1, result.Count());
    }

    [Fact]
    public async Task GetCompanyAsync_ValidId_ReturnsCompany()
    {
        // Arrange
        var companyId = 1;

        // Act
        var result = await _companyService.GetCompanyAsync(companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);
    }

    [Fact]
    public async Task GetCompanyAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var companyId = 99;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _companyService.GetCompanyAsync(companyId));
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
        var result = await _companyService.CreateCompanyAsync(createCompany);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal(createCompany.Name, result.Name);
    }

    [Fact]
    public async Task CreateCompanyAsync_ExistingCompany_ThrowsConflictException()
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
        await _companyService.CreateCompanyAsync(createCompany);
        
        // Assert
        await Assert.ThrowsAsync<ConflictException>(() => _companyService.CreateCompanyAsync(createCompany));
    }
    
    [Fact]
    public async Task UpdateCompanyAsync_ValidId_ReturnsUpdatedCompany()
    {
        // Arrange
        var companyId = 1;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
            Website = "https://updated-example.com"
        };

        // Act
        var result = await _companyService.UpdateCompanyAsync(companyId, updateCompany);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);
        Assert.Equal(updateCompany.Name, result.Name);
        Assert.Equal(updateCompany.Description, result.Description);
        Assert.Equal(updateCompany.Website, result.Website);
    }
    
    [Fact]
    public async Task UpdateCompanyAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var companyId = 99;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Logo = null,
            Website = "https://updated-example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _companyService.UpdateCompanyAsync(companyId, updateCompany));
    }
    
    [Fact]
    public async Task DeleteCompanyAsync_ValidId_DeletesCompany()
    {
        // Arrange
        var companyId = 1;

        // Act
        var result = await _companyService.DeleteCompanyAsync(companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(companyId, result.Id);

        var companyInDb = await Context.Company.FindAsync(companyId);
        Assert.Null(companyInDb);
    }
    
    [Fact]
    public async Task DeleteCompanyAsync_InvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var companyId = 99;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _companyService.DeleteCompanyAsync(companyId));
    }
    
    private async Task SeedDatabase()
    {
        if (!Context.Company.Any())
        {
            Context.Company.AddRange(GetTestCompanies());
            await Context.SaveChangesAsync();
        }
    }
    
    private static List<Company> GetTestCompanies()
    {
        var company = CreateCompany(1, "Test Company", "Test Description", "testLogo", "https://example.com");
        
        return [company];
    }
}