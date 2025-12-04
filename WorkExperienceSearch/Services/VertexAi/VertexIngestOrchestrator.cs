using Work_Experience_Search.Services.VertexAi;
using Work_Experience_Search.Services;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services.VertexAi;

public interface IVertexIngestOrchestrator
{
    Task<Result<VertexIngestSummary>> IngestAllAsync(CancellationToken cancellationToken = default);
}

public class VertexIngestOrchestrator(
    IProjectService projectService,
    ICompanyService companyService,
    ITagService tagService,
    IVertexIngestService vertexIngestService,
    IVertexChatbotClient vertexChatbotClient) : IVertexIngestOrchestrator
{
    public async Task<Result<VertexIngestSummary>> IngestAllAsync(CancellationToken cancellationToken = default)
    {
        await vertexChatbotClient.InitialiseCachesAsync(ensureSchema: true, cancellationToken);

        var companyResult = await companyService.GetCompaniesAsync(null);
        if (!companyResult.IsSuccess || companyResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch companies.");

        var tagResult = await tagService.GetTagsAsync(null);
        if (!tagResult.IsSuccess || tagResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch tags.");

        var projectResult = await projectService.GetProjectsAsync(null);
        if (!projectResult.IsSuccess || projectResult.Data == null) return new Failure<VertexIngestSummary>("Failed to fetch projects.");

        foreach (var company in companyResult.Data)
        {
            await vertexIngestService.UpsertCompanyAsync(company, cancellationToken);
        }

        foreach (var tag in tagResult.Data)
        {
            await vertexIngestService.UpsertTagAsync(tag, cancellationToken);
        }

        foreach (var project in projectResult.Data)
        {
            await vertexIngestService.UpsertProjectAsync(project, cancellationToken);
        }

        return new Success<VertexIngestSummary>(new VertexIngestSummary(
            CompaniesIngested: companyResult.Data.Count(),
            TagsIngested: tagResult.Data.Count(),
            ProjectsIngested: projectResult.Data.Count()));
    }
}

public record VertexIngestSummary(int CompaniesIngested, int TagsIngested, int ProjectsIngested);
