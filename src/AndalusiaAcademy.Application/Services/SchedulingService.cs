using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class SchedulingService(ISchedulingRepository repository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<TrainingLocationDto>> GetLocationsAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
        => repository.GetLocationsAsync(includeInactive, cancellationToken);

    public Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(
        Guid courseOfferingId,
        bool includeCancelled,
        CancellationToken cancellationToken)
        => repository.GetSessionsForOfferingAsync(courseOfferingId, includeCancelled, cancellationToken);

    public Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => repository.GetUpcomingSessionsForLearnerAsync(userId, timeProvider.GetUtcNow(), cancellationToken);

    public async Task<SchedulingMutationOutcome<TrainingLocationDto>> CreateLocationAsync(
        CreateTrainingLocationRequest request,
        CancellationToken cancellationToken)
    {
        var branchName = request.BranchName.Trim();
        var roomName = request.RoomName.Trim();
        var city = request.City.Trim();

        if (await repository.LocationNameExistsAsync(
            branchName, roomName, city, excludedId: null, cancellationToken))
        {
            return SchedulingMutationOutcome<TrainingLocationDto>.Failure(
                SchedulingFailureKind.Conflict,
                "A training location with the same branch, room, and city already exists.");
        }

        var location = new TrainingLocation
        {
            Id = Guid.NewGuid(),
            BranchName = branchName,
            RoomName = roomName,
            City = city,
            Address = request.Address.Trim(),
            MapUrl = NormalizeOptional(request.MapUrl),
            IsActive = true,
        };

        repository.AddLocation(location);
        await repository.SaveChangesAsync(cancellationToken);

        return SchedulingMutationOutcome<TrainingLocationDto>.Success(Map(location));
    }

    public async Task<SchedulingMutationOutcome<TrainingLocationDto>> UpdateLocationAsync(
        Guid id,
        UpdateTrainingLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationForUpdateAsync(id, cancellationToken);
        if (location is null)
        {
            return SchedulingMutationOutcome<TrainingLocationDto>.Failure(
                SchedulingFailureKind.NotFound,
                "Training location not found.");
        }

        var branchName = request.BranchName.Trim();
        var roomName = request.RoomName.Trim();
        var city = request.City.Trim();

        if (await repository.LocationNameExistsAsync(
            branchName, roomName, city, excludedId: id, cancellationToken))
        {
            return SchedulingMutationOutcome<TrainingLocationDto>.Failure(
                SchedulingFailureKind.Conflict,
                "A training location with the same branch, room, and city already exists.");
        }

        location.BranchName = branchName;
        location.RoomName = roomName;
        location.City = city;
        location.Address = request.Address.Trim();
        location.MapUrl = NormalizeOptional(request.MapUrl);
        location.IsActive = request.IsActive;

        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<TrainingLocationDto>.Success(Map(location));
    }

    public Task<SchedulingMutationOutcome<CourseSessionDto>> ScheduleSessionAsync(
        Guid courseOfferingId,
        ScheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            transactionCancellationToken => ScheduleSessionCoreAsync(
                courseOfferingId,
                request,
                transactionCancellationToken),
            cancellationToken);

    private async Task<SchedulingMutationOutcome<CourseSessionDto>> ScheduleSessionCoreAsync(
        Guid courseOfferingId,
        ScheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await ValidateSessionAsync(
            courseOfferingId,
            request.LocationId,
            request.InstructorUserId,
            request.StartsAtUtc,
            request.EndsAtUtc,
            excludedSessionId: null,
            cancellationToken);

        if (failure is not null)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(failure.Value.Kind, failure.Value.Error);
        }

        var session = new CourseSession
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = courseOfferingId,
            LocationId = request.LocationId,
            InstructorUserId = request.InstructorUserId,
            Title = request.Title.Trim(),
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            Status = CourseSessionStatuses.Scheduled,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        repository.AddSession(session);
        await repository.SaveChangesAsync(cancellationToken);

        var created = (await repository.GetSessionsForOfferingAsync(
            courseOfferingId, includeCancelled: true, cancellationToken))
            .Single(item => item.Id == session.Id);

        return SchedulingMutationOutcome<CourseSessionDto>.Success(created);
    }

    public Task<SchedulingMutationOutcome<CourseSessionDto>> RescheduleSessionAsync(
        Guid sessionId,
        RescheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            transactionCancellationToken => RescheduleSessionCoreAsync(
                sessionId,
                request,
                transactionCancellationToken),
            cancellationToken);

    private async Task<SchedulingMutationOutcome<CourseSessionDto>> RescheduleSessionCoreAsync(
        Guid sessionId,
        RescheduleCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(
                SchedulingFailureKind.NotFound,
                "Course session not found.");
        }

        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(
                SchedulingFailureKind.Conflict,
                "Only scheduled sessions can be changed.");
        }

        var failure = await ValidateSessionAsync(
            session.CourseOfferingId,
            request.LocationId,
            request.InstructorUserId,
            request.StartsAtUtc,
            request.EndsAtUtc,
            excludedSessionId: session.Id,
            cancellationToken);

        if (failure is not null)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(failure.Value.Kind, failure.Value.Error);
        }

        session.LocationId = request.LocationId;
        session.InstructorUserId = request.InstructorUserId;
        session.Title = request.Title.Trim();
        session.StartsAtUtc = request.StartsAtUtc;
        session.EndsAtUtc = request.EndsAtUtc;
        session.UpdatedAtUtc = timeProvider.GetUtcNow();

        await repository.SaveChangesAsync(cancellationToken);

        var updated = (await repository.GetSessionsForOfferingAsync(
            session.CourseOfferingId, includeCancelled: true, cancellationToken))
            .Single(item => item.Id == session.Id);

        return SchedulingMutationOutcome<CourseSessionDto>.Success(updated);
    }

    public async Task<SchedulingMutationOutcome<CourseSessionDto>> CancelSessionAsync(
        Guid sessionId,
        CancelCourseSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionForUpdateAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(
                SchedulingFailureKind.NotFound,
                "Course session not found.");
        }

        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return SchedulingMutationOutcome<CourseSessionDto>.Failure(
                SchedulingFailureKind.Conflict,
                "Only scheduled sessions can be cancelled.");
        }

        session.Status = CourseSessionStatuses.Cancelled;
        session.CancellationReason = request.Reason.Trim();
        session.UpdatedAtUtc = timeProvider.GetUtcNow();

        await repository.SaveChangesAsync(cancellationToken);

        var cancelled = (await repository.GetSessionsForOfferingAsync(
            session.CourseOfferingId, includeCancelled: true, cancellationToken))
            .Single(item => item.Id == session.Id);

        return SchedulingMutationOutcome<CourseSessionDto>.Success(cancelled);
    }

    private async Task<(SchedulingFailureKind Kind, string Error)?> ValidateSessionAsync(
        Guid courseOfferingId,
        Guid locationId,
        Guid? instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedSessionId,
        CancellationToken cancellationToken)
    {
        var offering = await repository.GetCourseOfferingForSchedulingAsync(courseOfferingId, cancellationToken);
        if (offering is null)
        {
            return (SchedulingFailureKind.NotFound, "Course offering not found.");
        }

        if (offering.Status == OfferingStatuses.Cancelled)
        {
            return (SchedulingFailureKind.Conflict, "Sessions cannot be added to a cancelled offering.");
        }

        var location = await repository.GetLocationForUpdateAsync(locationId, cancellationToken);
        if (location is null || !location.IsActive)
        {
            return (SchedulingFailureKind.Validation, "Select an active training location.");
        }

        if (startsAtUtc <= timeProvider.GetUtcNow())
        {
            return (SchedulingFailureKind.Validation, "A session must start in the future.");
        }

        if (startsAtUtc < offering.StartsAtUtc || endsAtUtc > offering.EndsAtUtc)
        {
            return (SchedulingFailureKind.Validation, "The session must fall within the offering dates.");
        }

        if (instructorUserId.HasValue &&
            !await repository.IsInstructorAsync(instructorUserId.Value, cancellationToken))
        {
            return (SchedulingFailureKind.Validation, "The selected user is not an instructor.");
        }

        if (await repository.HasLocationConflictAsync(
            locationId, startsAtUtc, endsAtUtc, excludedSessionId, cancellationToken))
        {
            return (SchedulingFailureKind.Conflict, "The training location is already booked during this time.");
        }

        if (instructorUserId.HasValue && await repository.HasInstructorConflictAsync(
            instructorUserId.Value, startsAtUtc, endsAtUtc, excludedSessionId, cancellationToken))
        {
            return (SchedulingFailureKind.Conflict, "The instructor is already assigned during this time.");
        }

        return null;
    }

    private static TrainingLocationDto Map(TrainingLocation location)
        => new(
            location.Id,
            location.BranchName,
            location.RoomName,
            location.City,
            location.Address,
            location.MapUrl,
            location.IsActive);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
