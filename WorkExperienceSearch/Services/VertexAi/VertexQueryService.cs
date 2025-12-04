using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexQueryService
{
    Task<VertexQueryResult> QueryAsync(string query, string? tenantId, CancellationToken cancellationToken = default);
}

public class VertexQueryService : IVertexQueryService
{
    private readonly HttpClient _httpClient;
    private readonly VertexAiOptions _options;
    private readonly GoogleCredential _credential;
    private readonly ILogger<VertexQueryService> _logger;

    public VertexQueryService(IHttpClientFactory httpClientFactory, IOptions<VertexAiOptions> options, ILogger<VertexQueryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(VertexQueryService));
        _options = options.Value;
        _logger = logger;
        _credential = BuildCredential(_options).CreateScoped("https://www.googleapis.com/auth/cloud-platform");
    }

    public async Task<VertexQueryResult> QueryAsync(string query, string? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = string.IsNullOrWhiteSpace(tenantId) ? _options.DefaultTenantId : tenantId;
        var dataStoreId = $"{tenant}_{_options.QueryDataStoreSuffix}";
        var datastoreResource = $"projects/{_options.ProjectId}/locations/{_options.Location}/collections/{_options.Collection}/dataStores/{dataStoreId}";
        var hostLocation = string.IsNullOrWhiteSpace(_options.ModelLocation) ? _options.Location : _options.ModelLocation;

        var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);

        var body = new
        {
            systemInstruction = new
            {
                role = "system",
                parts = new[]
                {
                    new { text = "You are a retrieval bot. Answer strictly using the retrieved context from the Vertex AI Search datastore. If the answer is not present in the retrieved context, reply with \"I don't have enough information to answer that.\" Keep answers concise and reference project titles where applicable." }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = query } }
                }
            },
            tools = new[]
            {
                new
                {
                    retrieval = new
                    {
                        vertexAiSearch = new
                        {
                            datastore = datastoreResource
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.0
            }
        };

        var url = $"https://{hostLocation}-aiplatform.googleapis.com/v1/projects/{_options.ProjectId}/locations/{hostLocation}/publishers/google/models/{_options.Model}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Vertex query failed: {Status} {Body}", response.StatusCode, raw);
            throw new InvalidOperationException($"Vertex query failed ({(int)response.StatusCode} {response.StatusCode}): {raw}");
        }

        var answer = ExtractAnswer(raw);
        var citations = ExtractCitations(raw);
        return new VertexQueryResult(answer ?? string.Empty, citations);
    }

    private static string? ExtractAnswer(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var candidate = doc.RootElement.GetPropertyOrDefault("candidates")?.EnumerateArray().FirstOrDefault();
        var parts = candidate?.GetPropertyOrDefault("content")?.GetPropertyOrDefault("parts");
        var textPart = parts?.EnumerateArray().FirstOrDefault(p => p.TryGetProperty("text", out _));
        return textPart?.GetProperty("text").GetString();
    }

    private static IReadOnlyList<VertexCitation> ExtractCitations(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<VertexCitation>();
        var candidate = doc.RootElement.GetPropertyOrDefault("candidates")?.EnumerateArray().FirstOrDefault();

        var groundingChunks = candidate?.GetPropertyOrDefault("groundingMetadata")?.GetPropertyOrDefault("groundingChunks");
        if (groundingChunks is { ValueKind: JsonValueKind.Array })
        {
            foreach (var chunk in groundingChunks.Value.EnumerateArray())
            {
                var context = chunk.GetPropertyOrDefault("retrievedContext");
                var documentName = context?.GetPropertyOrDefault("documentName")?.GetString();
                var text = context?.GetPropertyOrDefault("text")?.GetString();
                if (string.IsNullOrWhiteSpace(documentName)) continue;

                list.Add(new VertexCitation
                {
                    ProjectId = ExtractProjectId(documentName),
                    FeatureType = ExtractFeatureType(documentName),
                    Title = ExtractTitle(text)
                });
            }
        }

        var citation = candidate?.GetPropertyOrDefault("citationMetadata")?.GetPropertyOrDefault("citations");
        if (citation is { ValueKind: JsonValueKind.Array })
        {
            foreach (var c in citation.Value.EnumerateArray())
            {
                var uri = c.GetPropertyOrDefault("uri")?.GetString();
                var source = c.GetPropertyOrDefault("source")?.GetString();
                var documentName = uri ?? source;
                if (!string.IsNullOrWhiteSpace(documentName))
                {
                    list.Add(new VertexCitation
                    {
                        ProjectId = ExtractProjectId(documentName),
                        FeatureType = ExtractFeatureType(documentName),
                        Title = null
                    });
                }
            }
        }

        return list;
    }

    private static int? ExtractProjectId(string documentName)
    {
        var parts = documentName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var docIndex = Array.IndexOf(parts, "documents");
        if (docIndex >= 0 && docIndex + 1 < parts.Length && int.TryParse(parts[docIndex + 1], out var id))
        {
            return id;
        }

        return null;
    }

    private static VertexFeatureType? ExtractFeatureType(string documentName)
    {
        var parts = documentName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dsIndex = Array.IndexOf(parts, "dataStores");
        if (dsIndex >= 0 && dsIndex + 1 < parts.Length)
        {
            var dataStore = parts[dsIndex + 1].ToLowerInvariant();
            if (dataStore.Contains("project")) return VertexFeatureType.Project;
            if (dataStore.Contains("company")) return VertexFeatureType.Company;
            if (dataStore.Contains("tag")) return VertexFeatureType.Tag;
        }

        return null;
    }

    private static string? ExtractTitle(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var lines = text.Split('\n');
        var titleLine = lines.FirstOrDefault(l => l.StartsWith("Title:", StringComparison.OrdinalIgnoreCase));
        return titleLine != null ? titleLine["Title:".Length..].Trim() : null;
    }

    private static GoogleCredential BuildCredential(VertexAiOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CredentialsFile))
        {
            return GoogleCredential.FromFile(options.CredentialsFile);
        }

        if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
        {
            return GoogleCredential.FromJson(options.CredentialsJson);
        }

        return GoogleCredential.GetApplicationDefault();
    }
}

public record VertexQueryResult(string Answer, IReadOnlyList<VertexCitation> Citations);

public record VertexCitation
{
    public int? ProjectId { get; init; }
    public VertexFeatureType? FeatureType { get; init; }
    public string? Title { get; init; }
}

internal static class JsonExtensions
{
    public static JsonElement? GetPropertyOrDefault(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) ? value : null;
    }
}
