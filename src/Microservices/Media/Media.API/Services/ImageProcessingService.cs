using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Media.API.Services;

public sealed class ImageProcessingService
{
    private const int ThumbnailMaxWidth = 200;
    private const int ThumbnailMaxHeight = 200;

    public async Task<Stream> CreateThumbnailAsync(
        Stream inputStream, CancellationToken ct)
    {
        var output = new MemoryStream();

        // Reset stream position in case it was read before
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var image = await Image.LoadAsync(inputStream, ct);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ThumbnailMaxWidth, ThumbnailMaxHeight),
            Mode = ResizeMode.Max
        }));

        await image.SaveAsJpegAsync(output, ct);
        output.Position = 0;
        return output;
    }
}
