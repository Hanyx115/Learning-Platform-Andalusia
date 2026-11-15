using AndalusiaAcademy.Application.DTOs.Assignments;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface IAssignmentRepository
{
    Task<bool> OfferingExistsAsync(Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    /// <summary>True if the user is assigned as instructor to at least one session within the offering.</summary>
    Task<bool> IsInstructorForOfferingAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    Task<bool> IsEnrolledAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    void Add(Assignment assignment);

    Task<Assignment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<AssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssignmentDto>> GetForOfferingAsync(
        Guid? courseOfferingId, Guid? programOfferingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SubmissionRecordDto>> GetRosterAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid userId, CancellationToken cancellationToken);

    Task UpsertSubmissionAsync(
        Guid assignmentId,
        Guid userId,
        AssignmentSubmissionWriteModel submission,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken);

    Task GradeSubmissionAsync(
        Guid assignmentId,
        Guid userId,
        Guid gradedByUserId,
        decimal score,
        string? feedback,
        DateTimeOffset gradedAtUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LearnerAssignmentDto>> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
