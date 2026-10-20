using System.Linq.Expressions;
using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Repositories;

public sealed class ApplicationRepository(ApplicationDbContext dbContext) : IApplicationRepository
{
    private static readonly Expression<Func<LearnerApplication, ApplicationDto>> ToDto = application => new ApplicationDto(
        application.Id,
        application.UserId,
        application.CourseOfferingId,
        application.CourseOffering != null ? application.CourseOffering.CourseId : null,
        application.CourseOffering != null ? application.CourseOffering.Course!.Title : null,
        application.ProgramOfferingId,
        application.ProgramOffering != null ? application.ProgramOffering.ProgramId : null,
        application.ProgramOffering != null ? application.ProgramOffering.Program!.Title : null,
        application.CourseOffering != null
            ? application.CourseOffering.StartsAtUtc
            : application.ProgramOffering!.StartsAtUtc,
        application.CourseOffering != null
            ? application.CourseOffering.EndsAtUtc
            : application.ProgramOffering!.EndsAtUtc,
        application.CourseOffering != null
            ? application.CourseOffering.Price
            : application.ProgramOffering!.Price,
        application.CourseOffering != null
            ? application.CourseOffering.Currency
            : application.ProgramOffering!.Currency,
        application.Status,
        application.SubmittedAt,
        application.ReviewedAt,
        application.RejectionReason,
        application.StatusHistory
            .OrderBy(history => history.ChangedAt)
            .Select(history => new ApplicationStatusHistoryDto(
                history.Id,
                history.Status,
                history.ChangedAt,
                history.Note))
            .ToList());

    public async Task<bool> HasPendingApplicationAsync(
        Guid userId,
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.LearnerApplications
            .AsNoTracking()
            .AnyAsync(
                application => application.UserId == userId
                    && application.Status == ApplicationStatus.Pending
                    && application.CourseOfferingId == courseOfferingId
                    && application.ProgramOfferingId == programOfferingId,
                cancellationToken);
    }

    public async Task<ApplicationDto> AddAsync(
        Guid userId,
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken)
    {
        var application = new LearnerApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseOfferingId = courseOfferingId,
            ProgramOfferingId = programOfferingId,
            Status = ApplicationStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        dbContext.LearnerApplications.Add(application);
        dbContext.LearnerApplicationStatusHistories.Add(new LearnerApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Status = ApplicationStatus.Pending,
            ChangedAt = application.SubmittedAt,
            ChangedByUserId = userId,
            Note = "Application submitted.",
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(application.Id, cancellationToken)
            ?? throw new InvalidOperationException("The application was not found immediately after being created.");
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.LearnerApplications
            .AsNoTracking()
            .Where(application => application.UserId == userId)
            .OrderByDescending(application => application.SubmittedAt)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.LearnerApplications
            .AsNoTracking()
            .Where(application => application.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<ApplicationDto>> GetPageAsync(
        ApplicationListQuery query,
        CancellationToken cancellationToken)
    {
        var applications = dbContext.LearnerApplications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            applications = applications.Where(application => application.Status == status);
        }

        var totalCount = await applications.CountAsync(cancellationToken);

        var items = await applications
            .OrderByDescending(application => application.SubmittedAt)
            .Select(ToDto)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ApplicationDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ApplicationDto?> UpdateStatusIfPendingAsync(
        Guid id,
        string newStatus,
        Guid reviewedByUserId,
        string? rejectionReason,
        CancellationToken cancellationToken)
    {
        var reviewedAt = DateTimeOffset.UtcNow;

        // Atomic, SQL-level guard: this only applies if the row is still Pending, so two concurrent
        // reviews of the same application can't both succeed.
        var updatedRows = await dbContext.LearnerApplications
            .Where(application => application.Id == id && application.Status == ApplicationStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(application => application.Status, newStatus)
                    .SetProperty(application => application.ReviewedAt, reviewedAt)
                    .SetProperty(application => application.ReviewedByUserId, reviewedByUserId)
                    .SetProperty(application => application.RejectionReason, rejectionReason),
                cancellationToken);

        if (updatedRows == 0)
        {
            return null;
        }

        dbContext.LearnerApplicationStatusHistories.Add(new LearnerApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = id,
            Status = newStatus,
            ChangedAt = reviewedAt,
            ChangedByUserId = reviewedByUserId,
            Note = rejectionReason,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }
}
