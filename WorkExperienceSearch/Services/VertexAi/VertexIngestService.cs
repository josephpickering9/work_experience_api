using System.Linq;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexIngestService
{
    Task UpsertProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task UpsertTagAsync(Tag tag, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);
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
            ensureSchema: false,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
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
            ensureSchema: false,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
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
            ensureSchema: false,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        await _chatbotClient.DeleteFeatureAsync(VertexFeatureType.Tag, tagId.ToString(), cancellationToken);
    }

    private static VertexProjectDto FlattenProject(Project project) =>
        new()
        {
            Id = project.Id,
            Title = project.Title,
            ShortDescription = project.ShortDescription,
            Description = project.Description,
            Year = project.Year,
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
            Description = company.Description,
            Website = company.Website
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
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Year { get; init; }
    public string? Website { get; init; }
    public Guid? CompanyId { get; init; }
    public string? Company { get; init; }
    public List<string> Tags { get; init; } = [];
    public List<string> TagTypes { get; init; } = [];
}

public class VertexCompanyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Website { get; init; }
}

public class VertexTagDto
{
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
