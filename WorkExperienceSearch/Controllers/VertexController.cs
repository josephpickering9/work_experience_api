using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Services.VertexAi;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class VertexController(
    IVertexIngestOrchestrator ingestOrchestrator,
    IVertexQueryService queryService,
    IVertexProjectDescriptionService projectDescriptionService
) : ControllerBase {

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(CancellationToken cancellationToken)
    {
        var result = await ingestOrchestrator.IngestAllAsync(cancellationToken);
        return result.ToResponse();
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] VertexQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await queryService.QueryAsync(request.Query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("projects/{id:guid}/description/suggest")]
    public async Task<ActionResult<ProjectDescriptionSuggestionResponse>> SuggestProjectDescription(ProjectId id, [FromBody] SuggestProjectDescriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await projectDescriptionService.SuggestDescriptionAsync(id, request, cancellationToken);
        return result.ToResponse();
    }
}

public record VertexQueryRequest(string Query);
