using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Services.VertexAi;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("vertex/ingest")]
public class VertexIngestController(
    IVertexIngestOrchestrator ingestOrchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestAll([FromQuery] string? tenantId, CancellationToken cancellationToken)
    {
        var result = await ingestOrchestrator.IngestAllAsync(tenantId, cancellationToken);
        return result.ToResponse();
    }
}
