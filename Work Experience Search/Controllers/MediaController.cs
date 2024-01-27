using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class MediaController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public MediaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("uploads/{fileName}")]
    public IActionResult GetFile(string fileName)
    {
        var filePath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "wwwroot", "uploads", fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        new FileExtensionContentTypeProvider().TryGetContentType(filePath, out var contentType);

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, contentType ?? "application/octet-stream");
    }
}
