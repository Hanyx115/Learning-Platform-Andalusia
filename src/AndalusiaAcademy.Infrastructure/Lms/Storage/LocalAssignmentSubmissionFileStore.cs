using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AndalusiaAcademy.Infrastructure.Lms.Storage;

public sealed class LocalAssignmentSubmissionFileStore : IAssignmentSubmissionFileStore
{
    private readonly string _storageRoot;

    public LocalAssignmentSubmissionFileStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["AssignmentSubmissions:StoragePath"];
        _storageRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "assignment-submissions")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<StoredAssignmentSubmissionFile> StoreAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var safeOriginalFileName = Path.GetFileName(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(safeOriginalFileName).ToLowerInvariant()}";
        var destinationPath = GetStoragePath(storedFileName);

        await using (var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await content.CopyToAsync(destination, cancellationToken);
        }

        var sizeBytes = new FileInfo(destinationPath).Length;
        return new StoredAssignmentSubmissionFile(storedFileName, safeOriginalFileName, contentType, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetStoragePath(storedFileName);
        Stream? stream = File.Exists(path)
            ? new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetStoragePath(storedFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetStoragePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            !string.Equals(storedFileName, Path.GetFileName(storedFileName), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The stored assignment file name is invalid.");
        }

        return Path.Combine(_storageRoot, storedFileName);
    }
}
