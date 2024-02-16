using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Work_Experience_Search.Tests;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

public class MediaControllerIntegrationTests(CustomWebApplicationFactory customWebApplicationFactory)
    : BaseControllerIntegrationTests(customWebApplicationFactory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetFile_ExistingFile_ReturnsFile()
    {
        // Arrange
        const string fileName = "testfile.txt";
        const string testContent = "This is a test file.";
        var webRoot = Factory.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        var filePath = Path.Combine(webRoot, "uploads", fileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Create a test file in the temporary directory
        await File.WriteAllTextAsync(filePath, testContent);

        // Act
        var response = await Client.GetAsync($"/media/uploads/{fileName}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(testContent, content);

        // Cleanup - delete the test file and directory after the test
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Directory.Delete(Path.GetDirectoryName(filePath)!, true);
        }
    }

    [Fact]
    public async Task GetFile_NonExistingFile_ReturnsNotFound()
    {
        // Arrange
        const string nonExistingFileName = "nonexistingfile.txt";

        // Act
        var response = await Client.GetAsync($"/media/uploads/{nonExistingFileName}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
}