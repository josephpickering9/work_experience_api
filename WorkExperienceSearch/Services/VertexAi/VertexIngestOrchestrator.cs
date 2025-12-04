using Work_Experience_Search.Services.VertexAi;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;
using Microsoft.Extensions.Options;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexIngestOrchestrator
{
    Task<Result<VertexIngestSummary>> IngestAllAsync(string? tenantId, CancellationToken cancellationToken = default);
}

public class VertexIngestOrchestrator(
    IProjectService projectService,
    ICompanyService companyService,
    ITagService tagService,
    IVertexIngestService vertexIngestService,
    IVertexChatbotClient vertexChatbotClient,
    IOptions<VertexAiOptions> vertexOptions) : IVertexIngestOrchestrator
{
    public async Task<Result<VertexIngestSummary>> IngestAllAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = !string.IsNullOrWhiteSpace(tenantId)
            ? tenantId
            : vertexOptions.Value.DefaultTenantId;

        await vertexChatbotClient.InitialiseCachesAsync(tenant, ensureSchema: true, cancellationToken);

        var companyResult = await companyService.GetCompaniesAsync(null);
        if (!companyResult.IsSuccess || companyResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch companies.");

        var tagResult = await tagService.GetTagsAsync(null);
        if (!tagResult.IsSuccess || tagResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch tags.");

        var projectResult = await projectService.GetProjectsAsync(null);
        if (!projectResult.IsSuccess || projectResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch projects.");

        foreach (var company in companyResult.Data)
        {
            await vertexIngestService.UpsertCompanyAsync(company, tenant, cancellationToken);
        }

        foreach (var tag in tagResult.Data)
        {
            await vertexIngestService.UpsertTagAsync(tag, tenant, cancellationToken);
        }

        foreach (var project in projectResult.Data)
        {
            await vertexIngestService.UpsertProjectAsync(project, tenant, cancellationToken);
        }

        return new Success<VertexIngestSummary>(new VertexIngestSummary(
            TenantId: tenant,
            CompaniesIngested: companyResult.Data.Count(),
            TagsIngested: tagResult.Data.Count(),
            ProjectsIngested: projectResult.Data.Count()));
    }
}

public record VertexIngestSummary(string TenantId, int CompaniesIngested, int TagsIngested, int ProjectsIngested);
