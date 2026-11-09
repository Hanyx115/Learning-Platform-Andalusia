using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class OfferingRepository(ApplicationDbContext dbContext) : IOfferingRepository
{
    public async Task<IReadOnlyList<OfferingDto>> GetOpenCourseOfferingsAsync(
        string courseSlug,
        DateTimeOffset availableAtUtc,
        CancellationToken cancellationToken)
        => await dbContext.CourseOfferings
            .AsNoTracking()
            .Where(offering =>
                offering.Course!.Slug == courseSlug &&
                offering.Status == OfferingStatuses.Open &&
                (offering.LocationId == null || offering.Location!.IsActive) &&
                offering.ApplicationOpensAtUtc <= availableAtUtc &&
                offering.ApplicationClosesAtUtc >= availableAtUtc)
            .OrderBy(offering => offering.StartsAtUtc)
            .Select(offering => new OfferingDto(
                offering.Id,
                offering.StartsAtUtc,
                offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc,
                offering.ApplicationClosesAtUtc,
                offering.Price,
                offering.Currency,
                offering.Capacity,
                offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id,
                    offering.Location.BranchName,
                    offering.Location.RoomName,
                    offering.Location.City,
                    offering.Location.Address,
                    offering.Location.MapUrl,
                    offering.Location.Capacity,
                    offering.Location.IsActive)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingDto>> GetOpenProgramOfferingsAsync(
        string programSlug,
        DateTimeOffset availableAtUtc,
        CancellationToken cancellationToken)
        => await dbContext.ProgramOfferings
            .AsNoTracking()
            .Where(offering =>
                offering.Program!.Slug == programSlug &&
                offering.Status == OfferingStatuses.Open &&
                (offering.LocationId == null || offering.Location!.IsActive) &&
                offering.ApplicationOpensAtUtc <= availableAtUtc &&
                offering.ApplicationClosesAtUtc >= availableAtUtc)
            .OrderBy(offering => offering.StartsAtUtc)
            .Select(offering => new OfferingDto(
                offering.Id,
                offering.StartsAtUtc,
                offering.EndsAtUtc,
                offering.ApplicationOpensAtUtc,
                offering.ApplicationClosesAtUtc,
                offering.Price,
                offering.Currency,
                offering.Capacity,
                offering.Status,
                offering.Location == null ? null : new TrainingLocationDto(
                    offering.Location.Id,
                    offering.Location.BranchName,
                    offering.Location.RoomName,
                    offering.Location.City,
                    offering.Location.Address,
                    offering.Location.MapUrl,
                    offering.Location.Capacity,
                    offering.Location.IsActive)))
            .ToListAsync(cancellationToken);

    public Task<OfferingTargetDto?> GetCourseOfferingAsync(Guid offeringId, CancellationToken cancellationToken)
        => dbContext.CourseOfferings
            .AsNoTracking()
            .Where(offering => offering.Id == offeringId &&
                (offering.LocationId == null || offering.Location!.IsActive))
            .Select(offering => new OfferingTargetDto(
                offering.Id,
                "Course",
                offering.Course!.Title,
                offering.Status,
                offering.ApplicationOpensAtUtc,
                offering.ApplicationClosesAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<OfferingTargetDto?> GetProgramOfferingAsync(Guid offeringId, CancellationToken cancellationToken)
        => dbContext.ProgramOfferings
            .AsNoTracking()
            .Where(offering => offering.Id == offeringId &&
                (offering.LocationId == null || offering.Location!.IsActive))
            .Select(offering => new OfferingTargetDto(
                offering.Id,
                "Program",
                offering.Program!.Title,
                offering.Status,
                offering.ApplicationOpensAtUtc,
                offering.ApplicationClosesAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
}
