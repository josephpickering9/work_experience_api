using System.Text.RegularExpressions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexIngestService
{
    Task UpsertProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default);

    Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(CompanyId companyId, CancellationToken cancellationToken = default);

    Task UpsertTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(TagId tagId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps domain models into flattened documents and upserts them into Vertex AI Search via IVertexChatbotClient.
/// </summary>
public class VertexIngestService : IVertexIngestService
{
    private readonly IVertexChatbotClient _chatbotClient;

    public VertexIngestService(IVertexChatbotClient chatbotClient)
    {
        _chatbotClient = chatbotClient;
    }

    public async Task UpsertProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        var dto = FlattenProject(project);
        var schema = VertexSchemaGenerator.GenerateSchema<VertexProjectDto>();
        await _chatbotClient.UpsertFeatureAsync(
            featureType: VertexFeatureType.Project,
            documentId: project.Id.ToString(),
            value: dto,
            jsonSchema: schema,
            ensureSchema: true,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        await _chatbotClient.DeleteFeatureAsync(VertexFeatureType.Project, projectId.ToString(), cancellationToken);
    }

    public async Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        var dto = FlattenCompany(company);
        var schema = VertexSchemaGenerator.GenerateSchema<VertexCompanyDto>();
        await _chatbotClient.UpsertFeatureAsync(
            featureType: VertexFeatureType.Company,
            documentId: company.Id.ToString(),
            value: dto,
            jsonSchema: schema,
            ensureSchema: true,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteCompanyAsync(CompanyId companyId, CancellationToken cancellationToken = default)
    {
        await _chatbotClient.DeleteFeatureAsync(VertexFeatureType.Company, companyId.ToString(), cancellationToken);
    }

    public async Task UpsertTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        var dto = FlattenTag(tag);
        var schema = VertexSchemaGenerator.GenerateSchema<VertexTagDto>();
        await _chatbotClient.UpsertFeatureAsync(
            featureType: VertexFeatureType.Tag,
            documentId: tag.Id.ToString(),
            value: dto,
            jsonSchema: schema,
            ensureSchema: true,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteTagAsync(TagId tagId, CancellationToken cancellationToken = default)
    {
        await _chatbotClient.DeleteFeatureAsync(VertexFeatureType.Tag, tagId.ToString(), cancellationToken);
    }

    private static string StripHtml(string html) =>
        Regex.Replace(html, "<[^>]*>", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", " ").Trim();

    private static VertexProjectDto FlattenProject(Project project) =>
        new()
        {
            Id = project.Id,
            Title = project.Title,
            ShortDescription = project.ShortDescription,
            Description = StripHtml(project.Description),
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Website = project.Website,
            Company = project.Company?.Name,
            CompanyId = project.CompanyId,
            Tags = project.Tags.Select(t => t.Title).ToList(),
            TagTypes = project.Tags.Select(t => t.Type.ToString()).ToList(),
        };

    private static VertexCompanyDto FlattenCompany(Company company) =>
        new()
        {
            Id = company.Id,
            Name = company.Name,
            Description = StripHtml(company.Description),
            Website = company.Website,
            StartDate = company.StartDate,
            EndDate = company.EndDate
        };

    private static VertexTagDto FlattenTag(Tag tag) =>
        new()
        {
            Title = tag.Title,
            Type = tag.Type.ToString()
        };
}

public class VertexProjectDto
{
    public ProjectId Id { get; init; }
    [VertexKeyProperty("title")] public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    [VertexKeyProperty("description")] public string Description { get; init; } = string.Empty;
    [VertexKeyProperty("createTime")] public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    [VertexKeyProperty("uri")] public string? Website { get; init; }
    public CompanyId? CompanyId { get; init; }
    [VertexKeyProperty("category")] public string? Company { get; init; }
    public List<string> Tags { get; init; } = [];
    public List<string> TagTypes { get; init; } = [];
}

public class VertexCompanyDto
{
    public CompanyId Id { get; init; }
    [VertexKeyProperty("title")] public string Name { get; init; } = string.Empty;
    [VertexKeyProperty("description")] public string Description { get; init; } = string.Empty;
    [VertexKeyProperty("uri")] public string? Website { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

public class VertexTagDto
{
    [VertexKeyProperty("title")] public string Title { get; init; } = string.Empty;
    [VertexKeyProperty("category")] public string Type { get; init; } = string.Empty;
}
