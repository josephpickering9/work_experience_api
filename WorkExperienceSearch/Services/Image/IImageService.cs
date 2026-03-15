using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services.Image;

public interface IImageService
{
    Result<ImageData> GetImage(string fileName);
    Task<Result<byte[]>> OptimiseImageAsync(byte[] file);
}
