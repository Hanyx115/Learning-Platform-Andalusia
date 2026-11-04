using AndalusiaAcademy.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AndalusiaAcademy.Infrastructure.Catalog.Storage;

public sealed class LocalCatalogImageFileStore : ICatalogImageFileStore
{
    private readonly string _storageRoot;

    public LocalCatalogImageFileStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["CatalogImages:StoragePath"];
        _storageRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "catalog-images")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> StoreAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = GetStoragePath(storedFileName);

        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(destination, cancellationToken);
        return storedFileName;
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetStoragePath(storedFileName);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous)
            : null;
        return Task.FromResult(stream);
    }

    private string GetStoragePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            !string.Equals(storedFileName, Path.GetFileName(storedFileName), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The stored catalog image name is invalid.");
        }

        return Path.Combine(_storageRoot, storedFileName);
    }
}
