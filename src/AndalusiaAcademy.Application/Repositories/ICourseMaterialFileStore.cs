using AndalusiaAcademy.Application.DTOs.Materials;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICourseMaterialFileStore
{
    Task<StoredCourseMaterialFile> StoreAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
