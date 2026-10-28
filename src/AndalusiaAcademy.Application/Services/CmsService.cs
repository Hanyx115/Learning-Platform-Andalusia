using AndalusiaAcademy.Application.Cms;
using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CmsService(ICmsRepository repository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<CmsContentItemDto>> GetPublishedAsync(
        string? contentType,
        CancellationToken cancellationToken)
        => repository.GetPublishedAsync(NormalizeOptional(contentType), cancellationToken);

    public Task<IReadOnlyList<CmsContentItemDto>> GetAllAsync(
        string? contentType,
        CancellationToken cancellationToken)
        => repository.GetAllAsync(NormalizeOptional(contentType), cancellationToken);

    public Task<CmsContentItemDto?> GetPublishedPageAsync(string key, CancellationToken cancellationToken)
        => repository.GetPublishedByKeyAsync(CmsContentTypes.PublicPage, key.Trim(), cancellationToken);

    public async Task<CmsMutationOutcome<CmsContentItemDto>> CreateAsync(
        UpsertCmsContentItemRequest request,
        Guid actingUserId,
        CancellationToken cancellationToken)
    {
        var contentType = request.ContentType.Trim();
        var key = request.Key.Trim().ToLowerInvariant();
        if (!CmsContentTypes.All.Contains(contentType))
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("Unsupported content type.");
        }

        if (await repository.KeyExistsAsync(contentType, key, null, cancellationToken))
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("A content item with this type and key already exists.");
        }

        var now = timeProvider.GetUtcNow();
        var item = new CmsContentItem
        {
            Id = Guid.NewGuid(),
            ContentType = contentType,
            Key = key,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedByUserId = actingUserId,
        };
        Apply(item, request);
        repository.Add(item);
        await repository.SaveChangesAsync(cancellationToken);

        return CmsMutationOutcome<CmsContentItemDto>.Success(
            (await repository.GetByIdAsync(item.Id, cancellationToken))!);
    }

    public async Task<CmsMutationOutcome<CmsContentItemDto>> UpdateAsync(
        Guid id,
        UpsertCmsContentItemRequest request,
        Guid actingUserId,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetForUpdateAsync(id, cancellationToken);
        if (item is null)
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("Content item not found.");
        }

        var contentType = request.ContentType.Trim();
        var key = request.Key.Trim().ToLowerInvariant();
        if (!CmsContentTypes.All.Contains(contentType))
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("Unsupported content type.");
        }

        if (await repository.KeyExistsAsync(contentType, key, id, cancellationToken))
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("A content item with this type and key already exists.");
        }

        item.ContentType = contentType;
        item.Key = key;
        item.UpdatedAtUtc = timeProvider.GetUtcNow();
        item.UpdatedByUserId = actingUserId;
        Apply(item, request);
        await repository.SaveChangesAsync(cancellationToken);

        return CmsMutationOutcome<CmsContentItemDto>.Success(
            (await repository.GetByIdAsync(item.Id, cancellationToken))!);
    }

    public async Task<CmsMutationOutcome<CmsContentItemDto>> SetPublicationAsync(
        Guid id,
        bool isPublished,
        Guid actingUserId,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetForUpdateAsync(id, cancellationToken);
        if (item is null)
        {
            return CmsMutationOutcome<CmsContentItemDto>.Failure("Content item not found.");
        }

        item.IsPublished = isPublished;
        item.UpdatedAtUtc = timeProvider.GetUtcNow();
        item.UpdatedByUserId = actingUserId;
        await repository.SaveChangesAsync(cancellationToken);

        return CmsMutationOutcome<CmsContentItemDto>.Success(
            (await repository.GetByIdAsync(item.Id, cancellationToken))!);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.GetForUpdateAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(CmsContentItem item, UpsertCmsContentItemRequest request)
    {
        item.TitleEn = request.TitleEn.Trim();
        item.TitleAr = request.TitleAr.Trim();
        item.BodyEn = request.BodyEn.Trim();
        item.BodyAr = request.BodyAr.Trim();
        item.ImageUrl = NormalizeOptional(request.ImageUrl);
        item.LinkUrl = NormalizeOptional(request.LinkUrl);
        item.RelatedUserId = request.RelatedUserId;
        item.DisplayOrder = request.DisplayOrder;
        item.IsPublished = request.IsPublished;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
