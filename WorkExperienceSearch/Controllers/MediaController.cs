using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class MediaController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("uploads/{fileName}")]
    public IActionResult GetFile(string fileName)
    {
        var filePath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "uploads", fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        new FileExtensionContentTypeProvider().TryGetContentType(filePath, out var contentType);

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, contentType ?? "application/octet-stream");
    }
}
