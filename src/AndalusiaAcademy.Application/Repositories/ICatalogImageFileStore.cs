namespace AndalusiaAcademy.Application.Repositories;

public interface ICatalogImageFileStore
{
    Task<string> StoreAsync(Stream content, string extension, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);
}
