using System.Linq.Expressions;
using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Content.Repositories;

public sealed class CmsRepository(ApplicationDbContext dbContext) : ICmsRepository
{
    private static readonly Expression<Func<CmsContentItem, CmsContentItemDto>> ToDto = item =>
        new CmsContentItemDto(
            item.Id,
            item.ContentType,
            item.Key,
            item.TitleEn,
            item.TitleAr,
            item.BodyEn,
            item.BodyAr,
            item.ImageUrl,
            item.LinkUrl,
            item.RelatedUserId,
            item.DisplayOrder,
            item.IsPublished,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);

    public async Task<IReadOnlyList<CmsContentItemDto>> GetPublishedAsync(
        string? contentType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CmsContentItems.AsNoTracking().Where(item => item.IsPublished);
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            query = query.Where(item => item.ContentType == contentType);
        }

        return await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Key)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CmsContentItemDto>> GetAllAsync(
        string? contentType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CmsContentItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            query = query.Where(item => item.ContentType == contentType);
        }

        return await query
            .OrderBy(item => item.ContentType)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.Key)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public Task<CmsContentItemDto?> GetPublishedByKeyAsync(
        string contentType,
        string key,
        CancellationToken cancellationToken)
        => dbContext.CmsContentItems
            .AsNoTracking()
            .Where(item => item.ContentType == contentType && item.Key == key && item.IsPublished)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CmsContentItem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CmsContentItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> KeyExistsAsync(
        string contentType,
        string key,
        Guid? excludedId,
        CancellationToken cancellationToken)
        => dbContext.CmsContentItems.AnyAsync(
            item => item.ContentType == contentType && item.Key == key &&
                    (!excludedId.HasValue || item.Id != excludedId.Value),
            cancellationToken);

    public void Add(CmsContentItem item) => dbContext.CmsContentItems.Add(item);

    public void Remove(CmsContentItem item) => dbContext.CmsContentItems.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public Task<CmsContentItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CmsContentItems.AsNoTracking().Where(item => item.Id == id).Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
}
