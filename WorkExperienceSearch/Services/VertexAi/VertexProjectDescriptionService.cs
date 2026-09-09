using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;
using System.Text.Json.Serialization;

using Work_Experience_Search.Repositories;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexProjectDescriptionService
{
    Task<Result<ProjectDescriptionSuggestionResponse>> SuggestDescriptionAsync(ProjectId projectId, SuggestProjectDescriptionRequest request, CancellationToken cancellationToken = default);
}

public class VertexProjectDescriptionService : IVertexProjectDescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly VertexAiOptions _options;
    private readonly GoogleCredential _credential;
    private readonly ILogger<VertexProjectDescriptionService> _logger;
    private readonly IProjectRepository _projectRepository;

    private const string SystemPrompt = """
You rewrite portfolio project descriptions for a personal developer site.

Your goals:
- Make each blurb concise, engaging, and focused on impact.
- Highlight the developer's role, key features, and notable technical decisions.
- Use first-person singular ("I") and past tense.
- Weave technologies naturally into the narrative.
- Prefer specific, concrete achievements (performance, usability, scale, automation) over vague claims.
- Avoid corporate jargon and filler phrases like "comprehensive platform", "stands as a testament", or "user-centric digital solutions".
- Do not invent features or metrics that are not supported by the input data.

Output format:
- Return a valid HTML snippet only (no <html>, <head>, or <body> tags).
- Wrap the content in a single root <div>.
- Inside, use one or two <p> elements for the summary, followed by a <ul> with 2–4 <li> items.
- Use simple semantic tags (<p>, <ul>, <li>, <strong>, <span>) and optional Tailwind / daisyUI utility classes such as "space-y-3", "text-sm", or "list-disc list-inside".
- Do not include a title or heading element (no <h1>–<h6>).
- Return raw HTML only; do NOT wrap the response in markdown or code fences.
""";

    private static readonly JsonSerializerOptions PromptSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public VertexProjectDescriptionService(
        IHttpClientFactory httpClientFactory,
        IOptions<VertexAiOptions> options,
        ILogger<VertexProjectDescriptionService> logger,
        IProjectRepository projectRepository)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(VertexProjectDescriptionService));
        _options = options.Value;
        _logger = logger;
        _projectRepository = projectRepository;
        _credential = VertexCredentialFactory.Create(_options, _logger).CreateScoped("https://www.googleapis.com/auth/cloud-platform");
    }

    public async Task<Result<ProjectDescriptionSuggestionResponse>> SuggestDescriptionAsync(ProjectId projectId, SuggestProjectDescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetAsync(projectId, cancellationToken);

        if (project == null) return new NotFoundFailure<ProjectDescriptionSuggestionResponse>("Project not found.");

        var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
        var hostLocation = string.IsNullOrWhiteSpace(_options.ModelLocation) ? _options.Location : _options.ModelLocation;

        var prompt = BuildPrompt(project, request);

        var body = new
        {
            systemInstruction = new
            {
                role = "system",
                parts = new[]
                {
                    new { text = SystemPrompt }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.35,
                topP = 0.9,
            }
        };

        var url = $"https://{hostLocation}-aiplatform.googleapis.com/v1/projects/{_options.ProjectId}/locations/{hostLocation}/publishers/google/models/{_options.Model}:generateContent";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vertex description generation failed: {Status} {Body}", response.StatusCode, raw);
            return new Failure<ProjectDescriptionSuggestionResponse>($"Vertex description generation failed ({(int)response.StatusCode} {response.StatusCode}).");
        }

        var answer = ExtractAnswer(raw);
        if (string.IsNullOrWhiteSpace(answer))
        {
            _logger.LogError("Vertex description generation returned no text. Raw response: {Body}", raw);
            return new Failure<ProjectDescriptionSuggestionResponse>("Vertex AI did not return a suggestion.");
        }

        return new Success<ProjectDescriptionSuggestionResponse>(ToResponse(project, answer.Trim()));
    }

    private static ProjectDescriptionSuggestionResponse ToResponse(Project project, string suggestedDescription) =>
        new(
            ProjectId: project.Id,
            Title: project.Title,
            ExistingDescription: project.Description,
            SuggestedDescription: suggestedDescription);

    private string BuildPrompt(Project project, SuggestProjectDescriptionRequest request)
    {
        var targetWordCount = request.TargetWordCount is > 0 ? request.TargetWordCount.Value : 170;
        var lowerBound = Math.Max(80, targetWordCount - 40);
        var upperBound = targetWordCount + 40;

        var structuredProject = new
        {
            project.Id,
            project.Title,
            project.ShortDescription,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Website,
            project.ShowMockup,
            Company = project.Company == null
                ? null
                : new
                {
                    project.Company.Id,
                    project.Company.Name,
                    project.Company.Description,
                    project.Company.Website,
                    project.Company.StartDate,
                    project.Company.EndDate
                },
            Tags = project.Tags.Select(t => new { t.Title, Type = t.Type.ToString(), t.Icon, t.CustomColour }),
            Repositories = project.Repositories.Select(r => new { r.Title, r.Url, r.Order }),
            Images = project.Images.Select(i => new { i.Type, i.Order, i.Image })
        };

        var projectJson = JsonSerializer.Serialize(structuredProject, PromptSerializerOptions);
        var extraContext = string.IsNullOrWhiteSpace(request.AdditionalContext) ? "None supplied." : request.AdditionalContext.Trim();
        var additionalData = request.AdditionalData is { Count: > 0 }
            ? JsonSerializer.Serialize(request.AdditionalData, PromptSerializerOptions)
            : "None supplied.";

        return $"""
Rewrite the project description for my personal portfolio site.

Requirements:
- Length: Aim for between {lowerBound} and {upperBound} words of visible text.
- Start with 1–2 sentences wrapped in <p> elements explaining what the product is, who it is for, and the main impact.
- After the summary paragraphs, include a <ul> with 2–4 <li> items focusing on:
  - What I personally did on the project.
  - 2–4 key features or technical achievements that made a difference.
  - How and where the main technologies were used.
- Use first-person singular ("I") and past tense.
- Use natural, straightforward language; avoid corporate buzzwords.
- The response MUST be HTML that can be rendered directly inside a Tailwind / daisyUI page. Do not include <html>, <head>, <body>, or a title element.
- Do not invent features, metrics, or company claims that are not in the data.

Here is the structured project data (source of truth – you may pull details from any of these fields, but do not hallucinate):
{projectJson}

Additional context from me:
{extraContext}

Additional structured data (may be empty):
{additionalData}
""";
    }

    private static string? ExtractAnswer(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidatesElement))
            return null;
            
        var candidate = candidatesElement.EnumerateArray().FirstOrDefault();
        if (candidate.ValueKind == JsonValueKind.Undefined)
            return null;
            
        if (!candidate.TryGetProperty("content", out var content))
            return null;
            
        if (!content.TryGetProperty("parts", out var parts))
            return null;
            
        if (parts.ValueKind == JsonValueKind.Array)
        {
            var textParts = parts
                .EnumerateArray()
                .Select(p =>
                {
                    if (p.TryGetProperty("text", out var text))
                        return text.GetString();
                    return null;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (textParts.Count > 0) return string.Join(string.Empty, textParts);
        }

        return null;
    }

}

public record ProjectDescriptionSuggestionResponse(ProjectId ProjectId, string Title, string ExistingDescription, string SuggestedDescription);

public class SuggestProjectDescriptionRequest
{
    public string? AdditionalContext { get; init; }
    public Dictionary<string, string>? AdditionalData { get; init; }
    public int? TargetWordCount { get; init; }
}
