using System.Net;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Models;
using Work_Experience_Search.Tests;
using Work_Experience_Search.Types;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

[Collection("Sequential")]
public class CompanyControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetCompanies_ReturnsCompanies()
    {
        var companies = new List<Company>
        {
            await CreateCompanyAsync(CompanyId.New(), "Company 1"),
            await CreateCompanyAsync(CompanyId.New(), "Company 2"),
            await CreateCompanyAsync(CompanyId.New(), "Company 3")
        };

        var httpResponse = await Client.GetAsync("/company");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var response = GetJsonContent<List<Company>>(stringResponse);

        Assert.NotNull(response);
        Assert.NotEmpty(response);
        Assert.Equal(companies.Count, response.Count);
    }

    [Fact]
    public async Task GetCompany_ExistingId_ReturnsCompany()
    {
        var testCompanyId = CompanyId.New();
        await CreateCompanyAsync(testCompanyId);

        var httpResponse = await Client.GetAsync($"/company/{testCompanyId}");
        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var company = GetJsonContent<Company>(stringResponse);

        Assert.NotNull(company);
        Assert.Equal(testCompanyId, company.Id);
    }

    [Fact]
    public async Task GetCompany_NonExistingId_ReturnsNotFound()
    {
        var nonExistingCompanyId = CompanyId.New();

        var httpResponse = await Client.GetAsync($"/company/{nonExistingCompanyId}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostCompany_Unauthorized_ReturnsUnauthorized()
    {
        var newCompany = new CreateCompany
        {
            Name = "New Company",
            Description = "New Description",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://newcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        var httpResponse = await Client.PostAsync("/company", content);

        Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PostCompany_Authorized_CreatesNewCompany()
    {
        var newCompany = new CreateCompany
        {
            Name = "Test Company",
            Description = "Test Description",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://testcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        var httpResponse = await AuthenticatedClient.PostAsync("/company", content);

        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualCompany = GetJsonContent<Company>(stringResponse);

        Assert.NotNull(actualCompany);
        Assert.Equal(newCompany.Name, actualCompany.Name);
        Assert.Equal(newCompany.Description, actualCompany.Description);
        Assert.Equal(newCompany.StartDate, actualCompany.StartDate);
        Assert.Equal(newCompany.EndDate, actualCompany.EndDate);
        Assert.Equal(newCompany.Website, actualCompany.Website);
    }

    [Fact]
    public async Task PostCompany_ExistingCompany_ReturnsConflict()
    {
        await CreateCompanyAsync(CompanyId.New(), "Conflict Company");
        var newCompany = new CreateCompany
        {
            Name = "Conflict Company",
            Description = "Test Description",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://testcompany.com"
        };
        var content = GetMultipartFormDataContent(newCompany);

        var httpResponse = await AuthenticatedClient.PostAsync("/company", content);

        Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
    }

    [Fact]
    public async Task PutCompany_ExistingId_UpdatesCompany()
    {
        var companyId = CompanyId.New();
        await CreateCompanyAsync(companyId);
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://updatedcompany.com"
        };
        var content = GetMultipartFormDataContent(updateCompany);

        var httpResponse = await AuthenticatedClient.PutAsync($"/company/{companyId}", content);

        httpResponse.EnsureSuccessStatusCode();
        var stringResponse = await httpResponse.Content.ReadAsStringAsync();
        var actualCompany = GetJsonContent<Company>(stringResponse);

        Assert.NotNull(actualCompany);
        Assert.Equal(companyId, actualCompany.Id);
        Assert.Equal(updateCompany.Name, actualCompany.Name);
        Assert.Equal(updateCompany.Description, actualCompany.Description);
        Assert.Equal(updateCompany.StartDate, actualCompany.StartDate);
        Assert.Equal(updateCompany.EndDate, actualCompany.EndDate);
        Assert.Equal(updateCompany.Website, actualCompany.Website);
    }

    [Fact]
    public async Task PutCompany_NonExistingId_ReturnsNotFound()
    {
        var nonExistingCompanyId = CompanyId.New();
        var updateCompany = new CreateCompany
        {
            Name = "Updated Company",
            Description = "Updated Description",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = new DateOnly(2021, 1, 1),
            Website = "https://updatedcompany.com"
        };
        var content = GetMultipartFormDataContent(updateCompany);

        var httpResponse = await AuthenticatedClient.PutAsync($"/company/{nonExistingCompanyId}", content);

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCompany_ExistingId_DeletesCompany()
    {
        var companyId = CompanyId.New();
        await CreateCompanyAsync(companyId);

        var httpResponse = await AuthenticatedClient.DeleteAsync($"/company/{companyId}");

        httpResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DeleteCompany_NonExistingId_ReturnsNotFound()
    {
        var nonExistingCompanyId = CompanyId.New();

        var httpResponse = await AuthenticatedClient.DeleteAsync($"/company/{nonExistingCompanyId}");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
    }
}
