using Newtonsoft.Json;
using System.Net;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Tests;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

public class CompanyControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetCompanies_ReturnsCompanies()
    {
        // Arrange
        var companies = new List<Company>
        {
            await CreateCompanyAsync(1, "Company 1"),
            await CreateCompanyAsync(2, "Company 2"),
            await CreateCompanyAsync(3, "Company 3")
        };

        // Act
        var httpResponse = await Client.GetAsync("/company");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = JsonConvert.DeserializeObject<List<Company>>(stringResponse);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(companies.Count, response.Count);
    }

    [Fact]
    public async Task GetCompany_ExistingId_ReturnsCompany()
    {
        // Arrange
        const int testCompanyId = 1;
        await CreateCompanyAsync(testCompanyId);

        // Act
        var httpResponse = await Client.GetAsync($"/company/{testCompanyId}");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var company = JsonConvert.DeserializeObject<Company>(stringResponse);

        // Assert
        Assert.NotNull(company);
        Assert.Equal(testCompanyId, company.Id);
    }

    [Fact]
    public async Task GetCompany_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingCompanyId = 999;

        // Act
        var httpResponse = await Client.GetAsync($"/company/{nonExistingCompanyId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostCompany_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var newCompany = new CreateCompany
        {
            Name = "New Company",
            Description = "New Description",
            Website = "https://newcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        // Act
        var httpResponse = await Client.PostAsync("/company", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostCompany_Authorized_CreatesNewCompany()
    {
        // Arrange
        var newCompany = new CreateCompany
        {
            Name = "Test Company",
            Description = "Test Description",
            Website = "https://testcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        // Act
        var httpResponse = await AuthenticatedClient.PostAsync("/company", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualCompany = JsonConvert.DeserializeObject<Company>(stringResponse);

        Assert.NotNull(actualCompany);
        Assert.Equal(newCompany.Name, actualCompany.Name);
        Assert.Equal(newCompany.Description, actualCompany.Description);
        Assert.Equal(newCompany.Website, actualCompany.Website);
    }

    [Fact]
    public async Task PostCompany_ExistingCompany_ReturnsConflict()
    {
        // Arrange
        await CreateCompanyAsync(1, name: "Conflict Company");
        var newCompany = new CreateCompany
        {
            Name = "Conflict Company",
            Description = "Test Description",
            Website = "https://testcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        // Act
        var httpResponse = await AuthenticatedClient.PostAsync("/company", content);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task PutCompany_ExistingId_UpdatesCompany()
    {
        // Arrange
        const int companyId = 1;
        await CreateCompanyAsync(companyId);
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Website = "https://updatedcompany.com"
        };
        var content = GetMultipartFormDataContent(updateCompany);

        // Act
        var httpResponse = await AuthenticatedClient.PutAsync($"/company/{companyId}", content);

        // Assert
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualCompany = JsonConvert.DeserializeObject<Company>(stringResponse);

        Assert.NotNull(actualCompany);
        Assert.Equal(companyId, actualCompany.Id);
        Assert.Equal(updateCompany.Name, actualCompany.Name);
        Assert.Equal(updateCompany.Description, actualCompany.Description);
        Assert.Equal(updateCompany.Website, actualCompany.Website);
    }
    
    [Fact]
    public async Task PutCompany_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingCompanyId = 999;
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            Website = "https://updatedcompany.com"
        };
        var content = GetMultipartFormDataContent(updateCompany);

        // Act
        var httpResponse = await AuthenticatedClient.PutAsync($"/company/{nonExistingCompanyId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteCompany_ExistingId_DeletesCompany()
    {
        // Arrange
        const int companyId = 1;
        await CreateCompanyAsync(companyId);
        
        // Act
        var httpResponse = await AuthenticatedClient.DeleteAsync($"/company/{companyId}");

        // Assert
        httpResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteCompany_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingCompanyId = 999;

        // Act
        var httpResponse = await AuthenticatedClient.DeleteAsync($"/company/{nonExistingCompanyId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
}
