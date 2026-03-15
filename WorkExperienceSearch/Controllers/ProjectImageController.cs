using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectImageController(IProjectImageService projectImageService) : ControllerBase
{
    [HttpPut("optimise")]
    [Authorize]
    public async Task<ActionResult> OptimiseImages()
    {
        var result = await projectImageService.OptimiseImagesAsync();
        return result.ToResponse();
    }
}
