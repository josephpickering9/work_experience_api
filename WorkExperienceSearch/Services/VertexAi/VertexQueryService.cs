using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Api.Gax.Grpc;
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
        return new VertexQueryResult(answer ?? string.Empty, citations, raw);
    }

    private static string? ExtractAnswer(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var candidate = doc.RootElement.GetPropertyOrDefault("candidates")?.EnumerateArray().FirstOrDefault();
        var parts = candidate?.GetPropertyOrDefault("content")?.GetPropertyOrDefault("parts");
        var textPart = parts?.EnumerateArray().FirstOrDefault(p => p.TryGetProperty("text", out _));
        return textPart?.GetProperty("text").GetString();
    }

    private static IReadOnlyList<string> ExtractCitations(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<string>();
        var candidate = doc.RootElement.GetPropertyOrDefault("candidates")?.EnumerateArray().FirstOrDefault();
        var citation = candidate?.GetPropertyOrDefault("citationMetadata")?.GetPropertyOrDefault("citations");
        if (citation == null) return list;

        foreach (var c in citation.Value.EnumerateArray())
        {
            var uri = c.GetPropertyOrDefault("uri")?.GetString();
            var start = c.GetPropertyOrDefault("startIndex")?.GetInt32();
            var end = c.GetPropertyOrDefault("endIndex")?.GetInt32();
            var source = c.GetPropertyOrDefault("source")?.GetString();
            var label = uri ?? source;
            if (!string.IsNullOrEmpty(label))
            {
                list.Add(label);
            }
        }
        return list;
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

public record VertexQueryResult(string Answer, IReadOnlyList<string> Citations, string Raw);

internal static class JsonExtensions
{
    public static JsonElement? GetPropertyOrDefault(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) ? value : null;
    }
}
