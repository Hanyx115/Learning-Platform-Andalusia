using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CatalogImageService(ICatalogImageFileStore fileStore)
{
    private const long MaximumImageSizeBytes = 5 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
        };

    public async Task<CatalogImageUploadOutcome> UploadAsync(
        Stream content,
        string originalFileName,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        if (!ContentTypes.ContainsKey(extension))
        {
            return CatalogImageUploadOutcome.Failure("Choose a JPG, PNG, or WebP image.");
        }

        if (sizeBytes <= 0 || sizeBytes > MaximumImageSizeBytes)
        {
            return CatalogImageUploadOutcome.Failure("Choose a non-empty image no larger than 5 MB.");
        }

        await using var bufferedContent = new MemoryStream((int)sizeBytes);
        await content.CopyToAsync(bufferedContent, cancellationToken);
        if (bufferedContent.Length != sizeBytes || !HasExpectedSignature(bufferedContent, extension))
        {
            return CatalogImageUploadOutcome.Failure("The uploaded file is not a valid supported image.");
        }

        bufferedContent.Position = 0;
        var storedFileName = await fileStore.StoreAsync(bufferedContent, extension, cancellationToken);
        return CatalogImageUploadOutcome.Success(
            new CatalogImageDto($"/api/v1/catalog-images/{storedFileName}"));
    }

    public async Task<CatalogImageFile?> OpenAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(storedFileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            return null;
        }

        var content = await fileStore.OpenReadAsync(storedFileName, cancellationToken);
        return content is null ? null : new CatalogImageFile(content, contentType);
    }

    private static bool HasExpectedSignature(MemoryStream content, string extension)
    {
        var bytes = content.GetBuffer();
        var length = content.Length;

        if (extension is ".jpg" or ".jpeg")
        {
            return length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;
        }

        if (extension == ".png")
        {
            return length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
        }

        return length >= 12 &&
               bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
               bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
    }
}
