using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace Work_Experience_Search.Utils;

public static class FileExtensions
{
    public static async Task<byte[]> FileToByteArray(IFormFile image)
    {
        await using var imageStream = image.OpenReadStream();
        var buffer = new byte[image.Length];
        await imageStream.ReadAsync(buffer.AsMemory(0, (int)imageStream.Length));
        return buffer;
    }

    public static IFormFile ByteArrayToFile(byte[] fileBytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(fileBytes);
        var formFile = new FormFile(stream, 0, stream.Length, fileName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
        return formFile;
    }

    public static string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
}
