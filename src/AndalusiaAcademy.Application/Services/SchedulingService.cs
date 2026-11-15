using AndalusiaAcademy.Application.DTOs.Applications;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.Application.Services;

public sealed class SchedulingService(
    ISchedulingRepository repository,
    INotificationOutbox notificationOutbox,
    TimeProvider timeProvider,
    ILogger<SchedulingService> logger)
{
    public Task<IReadOnlyList<TrainingLocationDto>> GetLocationsAsync(bool includeInactive, CancellationToken cancellationToken)
        => repository.GetLocationsAsync(includeInactive, cancellationToken);

    public Task<IReadOnlyList<CourseSessionDto>> GetSessionsForOfferingAsync(Guid id, bool includeCancelled, CancellationToken cancellationToken)
        => repository.GetSessionsForOfferingAsync(id, includeCancelled, cancellationToken);

    public Task<IReadOnlyList<ProgramSessionDto>> GetSessionsForProgramOfferingAsync(Guid id, bool includeCancelled, CancellationToken cancellationToken)
        => repository.GetSessionsForProgramOfferingAsync(id, includeCancelled, cancellationToken);

    public Task<IReadOnlyList<UpcomingSessionDto>> GetUpcomingSessionsForLearnerAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetUpcomingSessionsForLearnerAsync(userId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<OfferingDto?> GetCourseOfferingDetailsAsync(Guid id, CancellationToken cancellationToken)
        => repository.GetCourseOfferingDetailsAsync(id, cancellationToken);

    public Task<OfferingDto?> GetProgramOfferingDetailsAsync(Guid id, CancellationToken cancellationToken)
        => repository.GetProgramOfferingDetailsAsync(id, cancellationToken);

    public Task<IReadOnlyList<OfferingDto>> GetOfferingsForCourseAsync(Guid courseId, CancellationToken cancellationToken)
        => repository.GetOfferingsForCourseAsync(courseId, cancellationToken);

    public Task<IReadOnlyList<OfferingDto>> GetOfferingsForProgramAsync(Guid programId, CancellationToken cancellationToken)
        => repository.GetOfferingsForProgramAsync(programId, cancellationToken);

    public Task<IReadOnlyList<SchedulingConflictDto>> GetConflictsAsync(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? locationId,
        Guid? instructorUserId,
        CancellationToken cancellationToken)
        => repository.GetConflictsAsync(
            startsAtUtc.ToUniversalTime(), endsAtUtc.ToUniversalTime(),
            locationId, instructorUserId, cancellationToken);

    public async Task<SchedulingMutationOutcome<TrainingLocationDto>> CreateLocationAsync(
        CreateTrainingLocationRequest request,
        CancellationToken cancellationToken)
    {
        var branchName = request.BranchName.Trim();
        var roomName = request.RoomName.Trim();
        var city = request.City.Trim();
        if (await repository.LocationNameExistsAsync(branchName, roomName, city, null, cancellationToken))
        {
            return Fail<TrainingLocationDto>(SchedulingFailureKind.Conflict,
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
            Capacity = request.Capacity,
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
            return Fail<TrainingLocationDto>(SchedulingFailureKind.NotFound, "Training location not found.");
        }

        var branchName = request.BranchName.Trim();
        var roomName = request.RoomName.Trim();
        var city = request.City.Trim();
        if (await repository.LocationNameExistsAsync(branchName, roomName, city, id, cancellationToken))
        {
            return Fail<TrainingLocationDto>(SchedulingFailureKind.Conflict,
                "A training location with the same branch, room, and city already exists.");
        }

        var requiredCapacity = await repository.GetMaximumOfferingCapacityForLocationAsync(id, cancellationToken);
        if (request.Capacity < requiredCapacity)
        {
            return Fail<TrainingLocationDto>(SchedulingFailureKind.Conflict,
                $"This location must retain capacity for at least {requiredCapacity} learners because an offering uses it.");
        }

        location.BranchName = branchName;
        location.RoomName = roomName;
        location.City = city;
        location.Address = request.Address.Trim();
        location.MapUrl = NormalizeOptional(request.MapUrl);
        location.Capacity = request.Capacity;
        location.IsActive = request.IsActive;
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<TrainingLocationDto>.Success(Map(location));
    }

    public Task<SchedulingMutationOutcome<OfferingDto>> CreateCourseOfferingAsync(
        Guid courseId, ManageOfferingRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            token => CreateCourseOfferingCoreAsync(courseId, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<OfferingDto>> CreateCourseOfferingCoreAsync(
        Guid courseId, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.CourseExistsAsync(courseId, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.NotFound, "Course not found.");
        }

        var normalized = Normalize(request);
        var failure = await ValidateOfferingAsync(normalized, cancellationToken);
        if (failure is not null)
        {
            return Fail<OfferingDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.CourseOfferingStartExistsAsync(courseId, normalized.StartsAtUtc, null, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "A course offering already starts at this time.");
        }

        var offering = new CourseOffering { Id = Guid.NewGuid(), CourseId = courseId, CreatedAtUtc = timeProvider.GetUtcNow() };
        Apply(offering, normalized);
        repository.AddCourseOffering(offering);
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<OfferingDto>.Success(
            (await repository.GetCourseOfferingDetailsAsync(offering.Id, cancellationToken))!);
    }

    public Task<SchedulingMutationOutcome<OfferingDto>> UpdateCourseOfferingAsync(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            token => UpdateCourseOfferingCoreAsync(id, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<OfferingDto>> UpdateCourseOfferingCoreAsync(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        var offering = await repository.GetCourseOfferingForSchedulingAsync(id, cancellationToken);
        if (offering is null)
        {
            return Fail<OfferingDto>(SchedulingFailureKind.NotFound, "Course offering not found.");
        }

        var normalized = Normalize(request);
        var failure = await ValidateOfferingAsync(normalized, cancellationToken);
        if (failure is not null)
        {
            return Fail<OfferingDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.CourseOfferingStartExistsAsync(offering.CourseId, normalized.StartsAtUtc, id, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict, "A course offering already starts at this time.");
        }

        if (await repository.HasCourseSessionsOutsidePeriodAsync(id, normalized.StartsAtUtc, normalized.EndsAtUtc, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "The new offering dates would leave one or more sessions outside the offering period.");
        }

        var committedSeats = await repository.GetCourseOfferingCommittedSeatCountAsync(id, cancellationToken);
        if (normalized.Capacity < committedSeats)
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                $"Capacity cannot be lower than the {committedSeats} enrolled or currently paying learners.");
        }

        if (offering.LocationId != normalized.LocationId &&
            await repository.CourseOfferingHasSessionsAsync(id, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "An offering location cannot be changed after its session schedule has been created.");
        }

        Apply(offering, normalized);
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<OfferingDto>.Success(
            (await repository.GetCourseOfferingDetailsAsync(id, cancellationToken))!);
    }

    public Task<SchedulingMutationOutcome<OfferingDto>> CreateProgramOfferingAsync(
        Guid programId, ManageOfferingRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            token => CreateProgramOfferingCoreAsync(programId, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<OfferingDto>> CreateProgramOfferingCoreAsync(
        Guid programId, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.ProgramExistsAsync(programId, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.NotFound, "Program not found.");
        }

        var normalized = Normalize(request);
        var failure = await ValidateOfferingAsync(normalized, cancellationToken);
        if (failure is not null)
        {
            return Fail<OfferingDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.ProgramOfferingStartExistsAsync(programId, normalized.StartsAtUtc, null, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "A program offering already starts at this time.");
        }

        var offering = new ProgramOffering { Id = Guid.NewGuid(), ProgramId = programId, CreatedAtUtc = timeProvider.GetUtcNow() };
        Apply(offering, normalized);
        repository.AddProgramOffering(offering);
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<OfferingDto>.Success(
            (await repository.GetProgramOfferingDetailsAsync(offering.Id, cancellationToken))!);
    }

    public Task<SchedulingMutationOutcome<OfferingDto>> UpdateProgramOfferingAsync(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(
            token => UpdateProgramOfferingCoreAsync(id, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<OfferingDto>> UpdateProgramOfferingCoreAsync(
        Guid id, ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        var offering = await repository.GetProgramOfferingForSchedulingAsync(id, cancellationToken);
        if (offering is null)
        {
            return Fail<OfferingDto>(SchedulingFailureKind.NotFound, "Program offering not found.");
        }

        var normalized = Normalize(request);
        var failure = await ValidateOfferingAsync(normalized, cancellationToken);
        if (failure is not null)
        {
            return Fail<OfferingDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.ProgramOfferingStartExistsAsync(offering.ProgramId, normalized.StartsAtUtc, id, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict, "A program offering already starts at this time.");
        }

        if (await repository.HasProgramSessionsOutsidePeriodAsync(id, normalized.StartsAtUtc, normalized.EndsAtUtc, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "The new offering dates would leave one or more sessions outside the offering period.");
        }

        var committedSeats = await repository.GetProgramOfferingCommittedSeatCountAsync(id, cancellationToken);
        if (normalized.Capacity < committedSeats)
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                $"Capacity cannot be lower than the {committedSeats} enrolled or currently paying learners.");
        }

        if (offering.LocationId != normalized.LocationId &&
            await repository.ProgramOfferingHasSessionsAsync(id, cancellationToken))
        {
            return Fail<OfferingDto>(SchedulingFailureKind.Conflict,
                "An offering location cannot be changed after its session schedule has been created.");
        }

        Apply(offering, normalized);
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingMutationOutcome<OfferingDto>.Success(
            (await repository.GetProgramOfferingDetailsAsync(id, cancellationToken))!);
    }

    public Task<SchedulingMutationOutcome<CourseSessionDto>> ScheduleSessionAsync(
        Guid offeringId, ScheduleCourseSessionRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(token => ScheduleCourseSessionCoreAsync(offeringId, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<CourseSessionDto>> ScheduleCourseSessionCoreAsync(
        Guid offeringId, ScheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var offering = await repository.GetCourseOfferingForSchedulingAsync(offeringId, cancellationToken);
        if (offering is null)
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.NotFound, "Course offering not found.");
        }

        var start = request.StartsAtUtc.ToUniversalTime();
        var end = request.EndsAtUtc.ToUniversalTime();
        var failure = await ValidateSessionAsync(offering.LocationId, offering.Capacity, offering.Status,
            offering.StartsAtUtc, offering.EndsAtUtc, request.LocationId, request.InstructorUserId,
            start, end, null, null, cancellationToken);
        if (failure is not null)
        {
            return Fail<CourseSessionDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.CourseSessionStartExistsAsync(offeringId, start, null, cancellationToken))
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.Conflict, "A duplicate course session already exists.");
        }

        var session = new CourseSession
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = offeringId,
            LocationId = request.LocationId,
            InstructorUserId = request.InstructorUserId,
            Title = request.Title.Trim(),
            StartsAtUtc = start,
            EndsAtUtc = end,
            Status = CourseSessionStatuses.Scheduled,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
        repository.AddSession(session);
        await repository.SaveChangesAsync(cancellationToken);
        await QueueInstructorAssignmentAsync(
            session.Id, session.InstructorUserId, session.Title, session.StartsAtUtc, cancellationToken);
        var created = (await repository.GetSessionsForOfferingAsync(offeringId, true, cancellationToken))
            .Single(item => item.Id == session.Id);
        return SchedulingMutationOutcome<CourseSessionDto>.Success(created);
    }

    public Task<SchedulingMutationOutcome<ProgramSessionDto>> ScheduleProgramSessionAsync(
        Guid offeringId, ScheduleCourseSessionRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(token => ScheduleProgramSessionCoreAsync(offeringId, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<ProgramSessionDto>> ScheduleProgramSessionCoreAsync(
        Guid offeringId, ScheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var offering = await repository.GetProgramOfferingForSchedulingAsync(offeringId, cancellationToken);
        if (offering is null)
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.NotFound, "Program offering not found.");
        }

        var start = request.StartsAtUtc.ToUniversalTime();
        var end = request.EndsAtUtc.ToUniversalTime();
        var failure = await ValidateSessionAsync(offering.LocationId, offering.Capacity, offering.Status,
            offering.StartsAtUtc, offering.EndsAtUtc, request.LocationId, request.InstructorUserId,
            start, end, null, null, cancellationToken);
        if (failure is not null)
        {
            return Fail<ProgramSessionDto>(failure.Value.Kind, failure.Value.Error);
        }

        if (await repository.ProgramSessionStartExistsAsync(offeringId, start, null, cancellationToken))
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.Conflict, "A duplicate program session already exists.");
        }

        var session = new ProgramSession
        {
            Id = Guid.NewGuid(),
            ProgramOfferingId = offeringId,
            LocationId = request.LocationId,
            InstructorUserId = request.InstructorUserId,
            Title = request.Title.Trim(),
            StartsAtUtc = start,
            EndsAtUtc = end,
            Status = CourseSessionStatuses.Scheduled,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
        repository.AddProgramSession(session);
        await repository.SaveChangesAsync(cancellationToken);
        await QueueInstructorAssignmentAsync(
            session.Id, session.InstructorUserId, session.Title, session.StartsAtUtc, cancellationToken);
        var created = (await repository.GetSessionsForProgramOfferingAsync(offeringId, true, cancellationToken))
            .Single(item => item.Id == session.Id);
        return SchedulingMutationOutcome<ProgramSessionDto>.Success(created);
    }

    public Task<SchedulingMutationOutcome<CourseSessionDto>> RescheduleSessionAsync(
        Guid id, RescheduleCourseSessionRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(token => RescheduleCourseSessionCoreAsync(id, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<CourseSessionDto>> RescheduleCourseSessionCoreAsync(
        Guid id, RescheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionForUpdateAsync(id, cancellationToken);
        if (session is null)
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.NotFound, "Course session not found.");
        }
        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.Conflict, "Only scheduled sessions can be changed.");
        }

        var offering = (await repository.GetCourseOfferingForSchedulingAsync(session.CourseOfferingId, cancellationToken))!;
        var start = request.StartsAtUtc.ToUniversalTime();
        var end = request.EndsAtUtc.ToUniversalTime();
        var failure = await ValidateSessionAsync(offering.LocationId, offering.Capacity, offering.Status,
            offering.StartsAtUtc, offering.EndsAtUtc, request.LocationId, request.InstructorUserId,
            start, end, session.Id, null, cancellationToken);
        if (failure is not null)
        {
            return Fail<CourseSessionDto>(failure.Value.Kind, failure.Value.Error);
        }
        if (await repository.CourseSessionStartExistsAsync(offering.Id, start, session.Id, cancellationToken))
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.Conflict, "A duplicate course session already exists.");
        }

        Apply(session, request, start, end);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = (await repository.GetSessionsForOfferingAsync(offering.Id, true, cancellationToken)).Single(item => item.Id == id);
        return SchedulingMutationOutcome<CourseSessionDto>.Success(updated);
    }

    public Task<SchedulingMutationOutcome<ProgramSessionDto>> RescheduleProgramSessionAsync(
        Guid id, RescheduleCourseSessionRequest request, CancellationToken cancellationToken)
        => repository.ExecuteSerializableAsync(token => RescheduleProgramSessionCoreAsync(id, request, token), cancellationToken);

    private async Task<SchedulingMutationOutcome<ProgramSessionDto>> RescheduleProgramSessionCoreAsync(
        Guid id, RescheduleCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await repository.GetProgramSessionForUpdateAsync(id, cancellationToken);
        if (session is null)
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.NotFound, "Program session not found.");
        }
        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.Conflict, "Only scheduled sessions can be changed.");
        }

        var offering = (await repository.GetProgramOfferingForSchedulingAsync(session.ProgramOfferingId, cancellationToken))!;
        var start = request.StartsAtUtc.ToUniversalTime();
        var end = request.EndsAtUtc.ToUniversalTime();
        var failure = await ValidateSessionAsync(offering.LocationId, offering.Capacity, offering.Status,
            offering.StartsAtUtc, offering.EndsAtUtc, request.LocationId, request.InstructorUserId,
            start, end, null, session.Id, cancellationToken);
        if (failure is not null)
        {
            return Fail<ProgramSessionDto>(failure.Value.Kind, failure.Value.Error);
        }
        if (await repository.ProgramSessionStartExistsAsync(offering.Id, start, session.Id, cancellationToken))
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.Conflict, "A duplicate program session already exists.");
        }

        Apply(session, request, start, end);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = (await repository.GetSessionsForProgramOfferingAsync(offering.Id, true, cancellationToken)).Single(item => item.Id == id);
        return SchedulingMutationOutcome<ProgramSessionDto>.Success(updated);
    }

    public async Task<SchedulingMutationOutcome<CourseSessionDto>> CancelSessionAsync(
        Guid id, CancelCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await repository.GetSessionForUpdateAsync(id, cancellationToken);
        if (session is null)
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.NotFound, "Course session not found.");
        }
        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return Fail<CourseSessionDto>(SchedulingFailureKind.Conflict, "Only scheduled sessions can be cancelled.");
        }
        Cancel(session, request.Reason);
        await repository.SaveChangesAsync(cancellationToken);
        var dto = (await repository.GetSessionsForOfferingAsync(session.CourseOfferingId, true, cancellationToken)).Single(item => item.Id == id);
        return SchedulingMutationOutcome<CourseSessionDto>.Success(dto);
    }

    public async Task<SchedulingMutationOutcome<ProgramSessionDto>> CancelProgramSessionAsync(
        Guid id, CancelCourseSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await repository.GetProgramSessionForUpdateAsync(id, cancellationToken);
        if (session is null)
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.NotFound, "Program session not found.");
        }
        if (session.Status != CourseSessionStatuses.Scheduled)
        {
            return Fail<ProgramSessionDto>(SchedulingFailureKind.Conflict, "Only scheduled sessions can be cancelled.");
        }
        Cancel(session, request.Reason);
        await repository.SaveChangesAsync(cancellationToken);
        var dto = (await repository.GetSessionsForProgramOfferingAsync(session.ProgramOfferingId, true, cancellationToken)).Single(item => item.Id == id);
        return SchedulingMutationOutcome<ProgramSessionDto>.Success(dto);
    }

    private async Task<(SchedulingFailureKind Kind, string Error)?> ValidateOfferingAsync(
        ManageOfferingRequest request, CancellationToken cancellationToken)
    {
        if (!request.LocationId.HasValue)
        {
            return null;
        }

        var location = await repository.GetLocationForUpdateAsync(request.LocationId.Value, cancellationToken);
        if (location is null || !location.IsActive)
        {
            return (SchedulingFailureKind.Validation, "Select an active training location.");
        }
        return location.Capacity < request.Capacity
            ? (SchedulingFailureKind.Validation, "The location capacity must support the offering capacity.")
            : null;
    }

    private async Task<(SchedulingFailureKind Kind, string Error)?> ValidateSessionAsync(
        Guid? offeringLocationId,
        int offeringCapacity,
        string offeringStatus,
        DateTimeOffset offeringStartsAtUtc,
        DateTimeOffset offeringEndsAtUtc,
        Guid? locationId,
        Guid instructorUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludedCourseSessionId,
        Guid? excludedProgramSessionId,
        CancellationToken cancellationToken)
    {
        if (offeringStatus == OfferingStatuses.Cancelled)
        {
            return (SchedulingFailureKind.Conflict, "Sessions cannot be added to a cancelled offering.");
        }
        if (startsAtUtc <= timeProvider.GetUtcNow())
        {
            return (SchedulingFailureKind.Validation, "A session must start in the future.");
        }
        if (startsAtUtc < offeringStartsAtUtc || endsAtUtc > offeringEndsAtUtc)
        {
            return (SchedulingFailureKind.Validation, "The session must fall within the offering dates.");
        }
        if (!await repository.IsInstructorAsync(instructorUserId, cancellationToken))
        {
            return (SchedulingFailureKind.Validation, "The selected user is not an instructor.");
        }

        if (!offeringLocationId.HasValue)
        {
            if (locationId.HasValue)
            {
                return (SchedulingFailureKind.Validation, "Online offerings cannot use a physical session location.");
            }
        }
        else
        {
            if (!locationId.HasValue)
            {
                return (SchedulingFailureKind.Validation, "A physical offering requires a session location.");
            }
            var offeringLocation = await repository.GetLocationForUpdateAsync(offeringLocationId.Value, cancellationToken);
            var location = await repository.GetLocationForUpdateAsync(locationId.Value, cancellationToken);
            if (location is null || !location.IsActive)
            {
                return (SchedulingFailureKind.Validation, "Select an active training location.");
            }
            if (offeringLocation is null || !string.Equals(location.City, offeringLocation.City, StringComparison.OrdinalIgnoreCase))
            {
                return (SchedulingFailureKind.Validation, "A session can only be scheduled within the offering city.");
            }
            if (location.Capacity < offeringCapacity)
            {
                return (SchedulingFailureKind.Validation, "The location capacity must support the offering capacity.");
            }
            if (await repository.HasLocationConflictAsync(location.Id, startsAtUtc, endsAtUtc,
                excludedCourseSessionId, excludedProgramSessionId, cancellationToken))
            {
                return (SchedulingFailureKind.Conflict, "The training location is already booked during this time.");
            }
        }

        if (await repository.HasInstructorConflictAsync(instructorUserId, startsAtUtc, endsAtUtc,
            excludedCourseSessionId, excludedProgramSessionId, cancellationToken))
        {
            return (SchedulingFailureKind.Conflict, "The instructor is already assigned during this time.");
        }
        return null;
    }

    private static ManageOfferingRequest Normalize(ManageOfferingRequest request)
        => request with
        {
            StartsAtUtc = request.StartsAtUtc.ToUniversalTime(),
            EndsAtUtc = request.EndsAtUtc.ToUniversalTime(),
            ApplicationOpensAtUtc = request.ApplicationOpensAtUtc.ToUniversalTime(),
            ApplicationClosesAtUtc = request.ApplicationClosesAtUtc.ToUniversalTime(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Status = request.Status.Trim(),
        };

    private static void Apply(CourseOffering offering, ManageOfferingRequest request)
    {
        offering.LocationId = request.LocationId;
        offering.StartsAtUtc = request.StartsAtUtc;
        offering.EndsAtUtc = request.EndsAtUtc;
        offering.ApplicationOpensAtUtc = request.ApplicationOpensAtUtc;
        offering.ApplicationClosesAtUtc = request.ApplicationClosesAtUtc;
        offering.Price = request.Price;
        offering.Currency = request.Currency;
        offering.Capacity = request.Capacity;
        offering.Status = request.Status;
    }

    private static void Apply(ProgramOffering offering, ManageOfferingRequest request)
    {
        offering.LocationId = request.LocationId;
        offering.StartsAtUtc = request.StartsAtUtc;
        offering.EndsAtUtc = request.EndsAtUtc;
        offering.ApplicationOpensAtUtc = request.ApplicationOpensAtUtc;
        offering.ApplicationClosesAtUtc = request.ApplicationClosesAtUtc;
        offering.Price = request.Price;
        offering.Currency = request.Currency;
        offering.Capacity = request.Capacity;
        offering.Status = request.Status;
    }

    private void Apply(CourseSession session, RescheduleCourseSessionRequest request, DateTimeOffset start, DateTimeOffset end)
    {
        session.LocationId = request.LocationId;
        session.InstructorUserId = request.InstructorUserId;
        session.Title = request.Title.Trim();
        session.StartsAtUtc = start;
        session.EndsAtUtc = end;
        session.ReschedulingNote = NormalizeOptional(request.Note);
        session.UpdatedAtUtc = timeProvider.GetUtcNow();
    }

    private void Apply(ProgramSession session, RescheduleCourseSessionRequest request, DateTimeOffset start, DateTimeOffset end)
    {
        session.LocationId = request.LocationId;
        session.InstructorUserId = request.InstructorUserId;
        session.Title = request.Title.Trim();
        session.StartsAtUtc = start;
        session.EndsAtUtc = end;
        session.ReschedulingNote = NormalizeOptional(request.Note);
        session.UpdatedAtUtc = timeProvider.GetUtcNow();
    }

    private void Cancel(CourseSession session, string reason)
    {
        session.Status = CourseSessionStatuses.Cancelled;
        session.CancellationReason = reason.Trim();
        session.UpdatedAtUtc = timeProvider.GetUtcNow();
    }

    private void Cancel(ProgramSession session, string reason)
    {
        session.Status = CourseSessionStatuses.Cancelled;
        session.CancellationReason = reason.Trim();
        session.UpdatedAtUtc = timeProvider.GetUtcNow();
    }

    private static TrainingLocationDto Map(TrainingLocation location)
        => new(location.Id, location.BranchName, location.RoomName, location.City,
            location.Address, location.MapUrl, location.Capacity, location.IsActive);

    private static SchedulingMutationOutcome<T> Fail<T>(SchedulingFailureKind kind, string error)
        where T : class
        => SchedulingMutationOutcome<T>.Failure(kind, error);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task QueueInstructorAssignmentAsync(
        Guid sessionId,
        Guid? instructorUserId,
        string title,
        DateTimeOffset startsAtUtc,
        CancellationToken cancellationToken)
    {
        if (!instructorUserId.HasValue)
        {
            return;
        }

        try
        {
            await notificationOutbox.EnqueueAsync(
                new NotificationEventRequest(
                    $"instructor-assigned:{sessionId:N}:{instructorUserId.Value:N}",
                    NotificationTemplateCodes.InstructorAssigned,
                    [instructorUserId.Value],
                    new Dictionary<string, string>
                    {
                        ["sessionTitle"] = title,
                        ["startsAt"] = startsAtUtc.ToString("u"),
                    },
                    "/instructor/sessions"),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Could not queue instructor assignment notification for session {SessionId}.", sessionId);
        }
    }
}
