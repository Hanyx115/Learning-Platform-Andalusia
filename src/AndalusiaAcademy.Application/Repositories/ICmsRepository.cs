using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICmsRepository
{
    Task<IReadOnlyList<CmsContentItemDto>> GetPublishedAsync(string? contentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<CmsContentItemDto>> GetAllAsync(string? contentType, CancellationToken cancellationToken);

    Task<CmsContentItemDto?> GetPublishedByKeyAsync(string contentType, string key, CancellationToken cancellationToken);

    Task<CmsContentItem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> KeyExistsAsync(string contentType, string key, Guid? excludedId, CancellationToken cancellationToken);

    void Add(CmsContentItem item);

    void Remove(CmsContentItem item);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<CmsContentItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
