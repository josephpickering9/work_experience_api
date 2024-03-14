using Microsoft.AspNetCore.StaticFiles;
using TinifyAPI;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services.Image;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _env;
    private const string TinifyApiKey = "6JrxCXTQVD04RBKj9GVXTQT67xnVSKky";

    public ImageService(IWebHostEnvironment hostEnvironment)
    {
        _env = hostEnvironment;
        Tinify.Key = TinifyApiKey;
    }

    public Result<ImageData> GetImage(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads", fileName);

            if (!File.Exists(filePath))
                return new NotFoundFailure<ImageData>("Image not found");

            new FileExtensionContentTypeProvider().TryGetContentType(filePath, out var contentType);

            var imageData = new ImageData
            {
                FileName = fileName,
                File = File.ReadAllBytes(filePath),
                ContentType = contentType ?? "application/octet-stream"
            };

            return new Success<ImageData>(imageData);
        }
        catch (Exception e)
        {
            return new Failure<ImageData>(e.Message);
        }
    }

    public async Task<Result<byte[]>> OptimiseImageAsync(byte[] image)
    {
        try
        {
            return new Success<byte[]>(await Tinify.FromBuffer(image).ToBuffer());
        }
        catch (TinifyException e)
        {
            return new Failure<byte[]>(e.Message);
        }
    }
}

public class ImageData
{
    public string FileName { get; set; }
    public byte[] File { get; set; }
    public string ContentType { get; set; }
}
