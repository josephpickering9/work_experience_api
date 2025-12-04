using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Services.VertexAi;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class VertexController(
    IVertexIngestOrchestrator ingestOrchestrator,
    IVertexQueryService queryService
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
}

public record VertexQueryRequest(string Query);
