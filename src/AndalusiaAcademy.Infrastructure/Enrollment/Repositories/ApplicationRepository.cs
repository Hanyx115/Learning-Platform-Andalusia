using System.Linq.Expressions;
using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Repositories;

public sealed class ApplicationRepository(ApplicationDbContext dbContext) : IApplicationRepository
{
    private Expression<Func<LearnerApplication, ApplicationDto>> ToDto => application => new ApplicationDto(
        application.Id,
        application.UserId,
        dbContext.Users
            .Where(user => user.Id == application.UserId)
            .Select(user => user.FirstName)
            .Single(),
        dbContext.Users
            .Where(user => user.Id == application.UserId)
            .Select(user => user.LastName)
            .Single(),
        dbContext.Users
            .Where(user => user.Id == application.UserId)
            .Select(user => user.Email!)
            .Single(),
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

    public async Task<bool> HasOpenApplicationAsync(
        Guid userId,
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken)
    {
        return await dbContext.LearnerApplications
            .AsNoTracking()
            .AnyAsync(
                application => application.UserId == userId
                    && (application.Status == ApplicationStatus.Pending ||
                        application.Status == ApplicationStatus.Accepted)
                    && application.CourseOfferingId == courseOfferingId
                    && application.ProgramOfferingId == programOfferingId,
                cancellationToken);
    }

    public async Task<ApplicationDto?> AddAsync(
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
        dbContext.Entry(application).Property<bool>("IntegrityProtected").CurrentValue = true;
        dbContext.LearnerApplicationStatusHistories.Add(new LearnerApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Status = ApplicationStatus.Pending,
            ChangedAt = application.SubmittedAt,
            ChangedByUserId = userId,
            Note = "Application submitted.",
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The filtered unique index is the final concurrency guard when two requests pass the
            // pre-check at the same time. Return a normal duplicate result instead of leaking a 500.
            dbContext.ChangeTracker.Clear();
            return null;
        }

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

        if (!string.IsNullOrWhiteSpace(query.LearnerName))
        {
            var learnerName = query.LearnerName.Trim();
            applications = applications.Where(application =>
                dbContext.Users.Any(user =>
                    user.Id == application.UserId &&
                    (user.FirstName.Contains(learnerName) ||
                     user.LastName.Contains(learnerName) ||
                     (user.FirstName + " " + user.LastName).Contains(learnerName))));
        }

        if (!string.IsNullOrWhiteSpace(query.OfferingTitle))
        {
            var offeringTitle = query.OfferingTitle.Trim();
            applications = applications.Where(application =>
                (application.CourseOffering != null &&
                 application.CourseOffering.Course!.Title.Contains(offeringTitle)) ||
                (application.ProgramOffering != null &&
                 application.ProgramOffering.Program!.Title.Contains(offeringTitle)));
        }

        if (query.IntakeFrom.HasValue)
        {
            var intakeFrom = new DateTimeOffset(
                query.IntakeFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            applications = applications.Where(application =>
                (application.CourseOffering != null && application.CourseOffering.StartsAtUtc >= intakeFrom) ||
                (application.ProgramOffering != null && application.ProgramOffering.StartsAtUtc >= intakeFrom));
        }

        if (query.IntakeTo.HasValue)
        {
            var intakeTo = new DateTimeOffset(
                query.IntakeTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            applications = applications.Where(application =>
                (application.CourseOffering != null && application.CourseOffering.StartsAtUtc <= intakeTo) ||
                (application.ProgramOffering != null && application.ProgramOffering.StartsAtUtc <= intakeTo));
        }

        var totalCount = await applications.CountAsync(cancellationToken);

        var offset = (long)(query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return new PaginatedResult<ApplicationDto>([], query.Page, query.PageSize, totalCount);
        }

        IOrderedQueryable<LearnerApplication> orderedApplications = query.Sort switch
        {
            "learner_asc" => applications
                .OrderBy(application => dbContext.Users
                    .Where(user => user.Id == application.UserId)
                    .Select(user => user.FirstName)
                    .Single())
                .ThenBy(application => dbContext.Users
                    .Where(user => user.Id == application.UserId)
                    .Select(user => user.LastName)
                    .Single())
                .ThenBy(application => application.Id),
            "learner_desc" => applications
                .OrderByDescending(application => dbContext.Users
                    .Where(user => user.Id == application.UserId)
                    .Select(user => user.FirstName)
                    .Single())
                .ThenByDescending(application => dbContext.Users
                    .Where(user => user.Id == application.UserId)
                    .Select(user => user.LastName)
                    .Single())
                .ThenBy(application => application.Id),
            "offering_asc" => applications
                .OrderBy(application => application.CourseOffering != null
                    ? application.CourseOffering.Course!.Title
                    : application.ProgramOffering!.Program!.Title)
                .ThenBy(application => application.Id),
            "offering_desc" => applications
                .OrderByDescending(application => application.CourseOffering != null
                    ? application.CourseOffering.Course!.Title
                    : application.ProgramOffering!.Program!.Title)
                .ThenBy(application => application.Id),
            "intake_asc" => applications
                .OrderBy(application => application.CourseOffering != null
                    ? application.CourseOffering.StartsAtUtc
                    : application.ProgramOffering!.StartsAtUtc)
                .ThenBy(application => application.Id),
            "intake_desc" => applications
                .OrderByDescending(application => application.CourseOffering != null
                    ? application.CourseOffering.StartsAtUtc
                    : application.ProgramOffering!.StartsAtUtc)
                .ThenBy(application => application.Id),
            _ => applications
                .OrderByDescending(application => application.SubmittedAt)
                .ThenBy(application => application.Id),
        };

        var items = await orderedApplications
            .Select(ToDto)
            .Skip((int)offset)
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
