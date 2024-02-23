using System.Net.Http.Headers;
using System.Text;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Tests;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

public class BaseControllerIntegrationTests : IAsyncLifetime
{
    private readonly IConfigurationSection _auth0Settings;
    protected readonly HttpClient AuthenticatedClient;
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;
    private static string _cachedToken = "";
    private static DateTime _tokenExpiryTime;

    protected BaseControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _auth0Settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("Auth0");

        Factory = factory;
        Client = Factory.CreateClient();
        AuthenticatedClient = GetAuthenticatedClient().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync()
    {
        await ClearDatabase();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task ClearDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();

        context.ProjectImage.RemoveRange(context.ProjectImage);
        context.ProjectTag.RemoveRange(context.ProjectTag);
        context.Project.RemoveRange(context.Project);
        context.Tag.RemoveRange(context.Tag);
        context.Company.RemoveRange(context.Company);
        await context.SaveChangesAsync();
    }

    protected async Task<Project> CreateProjectAsync(
        int projectId,
        string title = "Test Project",
        string description = "Test Description",
        string shortDescription = "Test Short Description",
        int companyId = 1,
        int year = 2021,
        string website = "https://example.com",
        List<string>? tags = null
    )
    {
        var project = new Project
        {
            Id = projectId,
            Title = title,
            Description = description,
            ShortDescription = shortDescription,
            CompanyId = companyId,
            Year = year,
            Website = website,
            Tags = []
        };

        using var scope = Factory.Services.CreateScope();

        if (tags?.Count > 0)
        {
            var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
            var createdTags = await tagService.SyncTagsAsync(tags);
            if (createdTags.Data != null) project.Tags = createdTags.Data;
        }

        var context = scope.ServiceProvider.GetRequiredService<Database>();

        context.Project.Add(project);
        await context.SaveChangesAsync();

        return project;
    }

    protected async Task<Tag> CreateTagAsync(
        int tagId,
        string title = "Test Tag",
        TagType type = TagType.Frontend,
        string? icon = null,
        string? customColour = null
    )
    {
        var tag = new Tag
        {
            Id = tagId,
            Title = title,
            Type = type,
            Icon = icon,
            CustomColour = customColour
        };

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();

        context.Tag.Add(tag);
        await context.SaveChangesAsync();

        return tag;
    }

    protected async Task<Company> CreateCompanyAsync(
        int companyId,
        string name = "Test Company",
        string description = "Test Description",
        string website = "https://example.com"
    )
    {
        var company = new Company
        {
            Id = companyId,
            Name = name,
            Description = description,
            Website = website
        };

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Database>();

        context.Company.Add(company);
        await context.SaveChangesAsync();

        return company;
    }

    protected static StringContent CreateJsonContent<T>(T data) where T : class
    {
        return new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
    }

    protected static T? GetJsonContent<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    protected static MultipartFormDataContent GetMultipartFormDataContent<T>(T data) where T : class
    {
        var content = new MultipartFormDataContent();

        foreach (var property in data.GetType().GetProperties())
        {
            var value = property.GetValue(data);
            if (value == null) continue;

            if (value is string stringValue)
                content.Add(new StringContent(stringValue), property.Name);
            else if (value is List<string> stringListValue)
                foreach (var item in stringListValue)
                    content.Add(new StringContent(item), property.Name);
            else if (value is IFormFile fileValue)
                content.Add(new StreamContent(fileValue.OpenReadStream()), property.Name, fileValue.FileName);
            else if (value is int intValue)
                content.Add(new StringContent(intValue.ToString()), property.Name);
            else
                content.Add(new StringContent(JsonConvert.SerializeObject(value)), property.Name);
        }

        return content;
    }

    private async Task<string> GetAccessToken()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime)
        {
            return _cachedToken;
        }

        var auth0Client = new AuthenticationApiClient(new Uri(_auth0Settings["Domain"] ?? ""));
        var tokenRequest = new ClientCredentialsTokenRequest
        {
            ClientId = _auth0Settings["ApiClientId"],
            ClientSecret = _auth0Settings["ApiClientSecret"],
            Audience = _auth0Settings["Audience"]
        };
        var tokenResponse = await auth0Client.GetTokenAsync(tokenRequest);

        // Cache the new token and set an expiry time
        _cachedToken = tokenResponse.AccessToken;
        // Assuming token expires in 1 hour; adjust based on actual expiry
        _tokenExpiryTime = DateTime.UtcNow.AddHours(1);

        return _cachedToken;
    }


    private async Task<HttpClient> GetAuthenticatedClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());
        return client;
    }
}