using AndalusiaAcademy.Application.DTOs.Instructor;

namespace AndalusiaAcademy.Application.Repositories;

public interface IInstructorRepository
{
    Task<InstructorOverviewDto> GetOverviewAsync(
        Guid instructorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InstructorSessionDto>> GetSessionsAsync(Guid instructorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InstructorOfferingDto>> GetOfferingsAsync(Guid instructorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InstructorStudentDto>> GetStudentsAsync(Guid instructorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetInstructorUserIdsForOfferingAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken);
}
