using AndalusiaAcademy.Application.DTOs.Instructor;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

/// <summary>Self-scoped by the caller's own id, so unlike most services here there is no
/// ownership check to fail: an instructor's dashboard can only ever be empty, never forbidden.</summary>
public sealed class InstructorDirectoryService(IInstructorRepository repository)
{
    public Task<IReadOnlyList<InstructorSessionDto>> GetMySessionsAsync(Guid instructorUserId, CancellationToken cancellationToken)
        => repository.GetSessionsAsync(instructorUserId, cancellationToken);

    public Task<IReadOnlyList<InstructorOfferingDto>> GetMyOfferingsAsync(Guid instructorUserId, CancellationToken cancellationToken)
        => repository.GetOfferingsAsync(instructorUserId, cancellationToken);

    public Task<IReadOnlyList<InstructorStudentDto>> GetMyStudentsAsync(Guid instructorUserId, CancellationToken cancellationToken)
        => repository.GetStudentsAsync(instructorUserId, cancellationToken);
}
