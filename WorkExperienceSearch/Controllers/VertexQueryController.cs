using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Services.VertexAi;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("vertex/query")]
public class VertexQueryController(IVertexQueryService queryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Query([FromBody] VertexQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await queryService.QueryAsync(request.Query, request.TenantId, cancellationToken);
        return Ok(result);
    }
}

public record VertexQueryRequest(string Query, string? TenantId);
