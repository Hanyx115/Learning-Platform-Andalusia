using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Application.Services;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CatalogImageServiceTests
{
    [Fact]
    public async Task UploadAsync_AcceptsValidPngAndReturnsPublicUrl()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x01 };
        var store = new RecordingStore();
        var service = new CatalogImageService(store);

        var outcome = await service.UploadAsync(
            new MemoryStream(bytes),
            "course.png",
            bytes.Length,
            CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.Equal("/api/v1/catalog-images/stored.png", outcome.Value!.ImageUrl);
        Assert.Equal(".png", store.Extension);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnsupportedOrSpoofedFiles()
    {
        var service = new CatalogImageService(new RecordingStore());

        var unsupported = await service.UploadAsync(
            new MemoryStream([1, 2, 3]), "course.svg", 3, CancellationToken.None);
        var spoofed = await service.UploadAsync(
            new MemoryStream([1, 2, 3]), "course.png", 3, CancellationToken.None);

        Assert.False(unsupported.Succeeded);
        Assert.False(spoofed.Succeeded);
    }

    private sealed class RecordingStore : ICatalogImageFileStore
    {
        public string? Extension { get; private set; }

        public Task<string> StoreAsync(Stream content, string extension, CancellationToken cancellationToken)
        {
            Extension = extension;
            return Task.FromResult("stored.png");
        }

        public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
            => Task.FromResult<Stream?>(null);
    }
}
