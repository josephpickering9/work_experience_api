using System.Net.Http.Json;
using System.Text.Json;
using Work_Experience_Search.Controllers;
using Xunit;

namespace WorkExperienceSearchTests.Tests.Integration;

/// <summary>
/// Skips the test unless the VERTEX_INTEGRATION_TESTS environment variable is set to "true".
/// These tests call the real Vertex AI API and incur costs.
///
/// Requires a running API instance. Set VERTEX_TEST_BASE_URL to override (default: http://localhost:5105).
///
/// To run: VERTEX_INTEGRATION_TESTS=true dotnet test --filter "Category=VertexIntegration"
/// </summary>
public sealed class VertexIntegrationTheoryAttribute : TheoryAttribute
{
    private const string EnvVar = "VERTEX_INTEGRATION_TESTS";

    public VertexIntegrationTheoryAttribute()
    {
        if (!IsEnabled)
            Skip = $"Skipped by default. Set {EnvVar}=true to run Vertex AI integration tests (incurs cost).";
    }

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnvVar), "true", StringComparison.OrdinalIgnoreCase);
}

// Minimal DTO — avoids deserializing Project/Company/Tag with their custom typed IDs
file record VertexQueryResponse(string Answer);

[Trait("Category", "VertexIntegration")]
public class VertexQueryIntegrationTests : IDisposable
{
    private const string FallbackAnswer = "I don't have enough information to answer that.";
    private const string DefaultBaseUrl = "http://localhost:5105";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    public VertexQueryIntegrationTests()
    {
        var baseUrl = Environment.GetEnvironmentVariable("VERTEX_TEST_BASE_URL") ?? DefaultBaseUrl;
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public void Dispose() => _client.Dispose();

    /// <summary>
    /// Each entry: (prompt, expected keywords — at least one must appear in the answer).
    /// Keywords are checked case-insensitively.
    /// </summary>
    public static IEnumerable<object[]> PromptTestCases =>
    [
        ["What project is Joe most proud of?",                     new[] { "beatcovidne", "covid", "proud", "drummond" }],
        ["What React projects has Joe worked on?",                 new[] { "react", "client portal", "research portal", "asurafin" }],
        ["What's the longest project Joe has worked on?",          new[] { "project", "year", "month", "long", "duration", "asurafin", "drummond" }],
        ["Summarize Joe's work history.",                          new[] { "firstcircle", "asurafin", "drummond", "developer", "experience", "software", "worked" }],
        ["What technologies does Joe use most?",                   new[] { "react", "vue", "typescript", ".net", "c#", "python" }],
        ["What Python projects has Joe worked on?",                new[] { "python", "data extractor", "script", "automation", "firstcircle" }],
        ["What has Joe built with Next.js?",                       new[] { "next.js", "nextjs", "asurafin website" }],
        ["Tell me about Joe's .NET background.",                   new[] { ".net", "c#", "asp.net", "dotnet" }],
        ["Which projects use Vue?",                                new[] { "vue", "nuxt" }],
        ["Show me projects using TypeScript.",                     new[] { "typescript", "ts" }],
        ["What Flutter apps has Joe built?",                       new[] { "flutter", "intros", "mobile" }],
        ["Has Joe worked with databases like PostgreSQL?",         new[] { "postgresql", "postgres", "database" }],
        ["What about mobile development with Android?",            new[] { "android", "mobile", "kotlin", "flutter" }],
        ["What PHP projects has Joe worked on?",                   new[] { "php", "laravel", "yii" }],
        ["What fintech experience does Joe have?",                 new[] { "fintech", "firstcircle", "asurafin", "finance", "investment", "banking", "financial" }],
        ["What AI projects has Joe worked on?",                    new[] { "ai", "vertex", "openai", "data extractor", "document", "machine learning" }],
        ["Tell me about Joe's experience with GCP.",               new[] { "gcp", "google cloud", "google cloud platform", "vertex", "google" }],
        ["What authentication systems has Joe used?",              new[] { "auth0", "keycloak", "authentication", "oauth" }],
        ["Has Joe worked on any mobile apps?",                     new[] { "mobile", "flutter", "android", "app", "intros" }],
        ["What projects involved document processing?",            new[] { "document", "data extractor", "pdf", "form recognizer", "parsing" }],
        ["What's Joe's experience with Nuxt?",                     new[] { "nuxt", "vue", "ssr" }],
        ["Tell me about Joe's CI/CD experience.",                  new[] { "ci/cd", "github actions", "circle ci", "pipeline", "deployment", "devops" }],
        ["What Kotlin projects has Joe built?",                    new[] { "kotlin", "android", "jvm" }],
        ["Has Joe worked with WordPress or CMS platforms?",        new[] { "wordpress", "cms", "umbraco", "kentico", "content management" }],
    ];

    [VertexIntegrationTheory]
    [MemberData(nameof(PromptTestCases))]
    public async Task Query_ReturnsRelevantAnswer(string prompt, string[] expectedKeywords)
    {
        var response = await _client.PostAsJsonAsync("/vertex/query", new VertexQueryRequest(prompt));

        Assert.True(
            response.IsSuccessStatusCode,
            $"Request failed with {(int)response.StatusCode} {response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        var result = await response.Content.ReadFromJsonAsync<VertexQueryResponse>(JsonOptions);
        Assert.NotNull(result);

        var answer = result.Answer;
        Assert.False(string.IsNullOrWhiteSpace(answer), $"Answer was empty for prompt: {prompt}");
        Assert.DoesNotContain(FallbackAnswer, answer, StringComparison.OrdinalIgnoreCase);

        var matchedKeyword = expectedKeywords.FirstOrDefault(k =>
            answer.Contains(k, StringComparison.OrdinalIgnoreCase));

        Assert.True(
            matchedKeyword != null,
            $"""
             Prompt   : {prompt}
             Expected : at least one of [{string.Join(", ", expectedKeywords)}]
             Answer   : {answer}
             """);
    }
}
