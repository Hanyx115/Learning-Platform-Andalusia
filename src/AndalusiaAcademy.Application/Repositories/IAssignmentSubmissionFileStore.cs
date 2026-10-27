using AndalusiaAcademy.Application.DTOs.Assignments;

namespace AndalusiaAcademy.Application.Repositories;

public interface IAssignmentSubmissionFileStore
{
    Task<StoredAssignmentSubmissionFile> StoreAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
