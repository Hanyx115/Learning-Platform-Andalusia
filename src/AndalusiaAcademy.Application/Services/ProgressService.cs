using AndalusiaAcademy.Application.DTOs.Progress;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class ProgressService(IProgressRepository repository)
{
    public async Task<SchedulingMutationOutcome<IReadOnlyList<OfferingProgressRecordDto>>> GetForOfferingAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        Guid callerUserId,
        bool callerIsPrivileged,
        CancellationToken cancellationToken)
    {
        if (!await repository.OfferingExistsAsync(courseOfferingId, programOfferingId, cancellationToken))
        {
            return Fail<IReadOnlyList<OfferingProgressRecordDto>>(SchedulingFailureKind.NotFound, "Offering not found.");
        }

        if (!callerIsPrivileged &&
            !await repository.IsInstructorForOfferingAsync(callerUserId, courseOfferingId, programOfferingId, cancellationToken))
        {
            return Fail<IReadOnlyList<OfferingProgressRecordDto>>(SchedulingFailureKind.NotFound, "Offering not found.");
        }

        var records = await repository.GetForOfferingAsync(courseOfferingId, programOfferingId, cancellationToken);
        return SchedulingMutationOutcome<IReadOnlyList<OfferingProgressRecordDto>>.Success(records);
    }

    public Task<IReadOnlyList<LearnerProgressDto>> GetMyProgressAsync(Guid userId, CancellationToken cancellationToken)
        => repository.GetForLearnerAsync(userId, cancellationToken);

    private static SchedulingMutationOutcome<T> Fail<T>(SchedulingFailureKind kind, string error)
        where T : class
        => SchedulingMutationOutcome<T>.Failure(kind, error);
}
