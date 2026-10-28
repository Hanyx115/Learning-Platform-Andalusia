using AndalusiaAcademy.Application.DTOs.Instructor;
using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class InstructorRepository(ApplicationDbContext dbContext) : IInstructorRepository
{
    public async Task<IReadOnlyList<InstructorSessionDto>> GetSessionsAsync(
        Guid instructorUserId, CancellationToken cancellationToken)
    {
        var courseSessions = await dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => new InstructorSessionDto(
                session.Id,
                session.CourseOfferingId,
                null,
                "Course",
                session.CourseOffering!.Course!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);

        var programSessions = await dbContext.ProgramSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => new InstructorSessionDto(
                session.Id,
                null,
                session.ProgramOfferingId,
                "Program",
                session.ProgramOffering!.Program!.Title,
                session.Title,
                session.StartsAtUtc,
                session.EndsAtUtc,
                session.Status,
                session.Location == null ? null : new TrainingLocationDto(
                    session.Location.Id, session.Location.BranchName, session.Location.RoomName,
                    session.Location.City, session.Location.Address, session.Location.MapUrl,
                    session.Location.Capacity, session.Location.IsActive)))
            .ToListAsync(cancellationToken);

        return courseSessions
            .Concat(programSessions)
            .OrderByDescending(session => session.StartsAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<InstructorOfferingDto>> GetOfferingsAsync(
        Guid instructorUserId, CancellationToken cancellationToken)
    {
        var courseOfferingIds = await dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => session.CourseOfferingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var programOfferingIds = await dbContext.ProgramSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => session.ProgramOfferingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var courseOfferings = await dbContext.CourseOfferings
            .AsNoTracking()
            .Where(offering => courseOfferingIds.Contains(offering.Id))
            .Select(offering => new InstructorOfferingDto(
                offering.Id,
                null,
                offering.CourseId,
                "Course",
                offering.Course!.Title,
                offering.StartsAtUtc,
                offering.EndsAtUtc,
                offering.Status,
                dbContext.Enrollments.Count(enrollment =>
                    enrollment.CourseOfferingId == offering.Id && enrollment.Status == EnrollmentStatus.Active)))
            .ToListAsync(cancellationToken);

        var programOfferings = await dbContext.ProgramOfferings
            .AsNoTracking()
            .Where(offering => programOfferingIds.Contains(offering.Id))
            .Select(offering => new InstructorOfferingDto(
                null,
                offering.Id,
                null,
                "Program",
                offering.Program!.Title,
                offering.StartsAtUtc,
                offering.EndsAtUtc,
                offering.Status,
                dbContext.Enrollments.Count(enrollment =>
                    enrollment.ProgramOfferingId == offering.Id && enrollment.Status == EnrollmentStatus.Active)))
            .ToListAsync(cancellationToken);

        return courseOfferings
            .Concat(programOfferings)
            .OrderByDescending(offering => offering.StartsAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<InstructorStudentDto>> GetStudentsAsync(
        Guid instructorUserId, CancellationToken cancellationToken)
    {
        var courseOfferingIds = await dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => session.CourseOfferingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var programOfferingIds = await dbContext.ProgramSessions
            .AsNoTracking()
            .Where(session => session.InstructorUserId == instructorUserId)
            .Select(session => session.ProgramOfferingId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var courseStudents = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.CourseOfferingId != null
                && courseOfferingIds.Contains(enrollment.CourseOfferingId.Value)
                && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new InstructorStudentDto(
                enrollment.UserId,
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.Email!).Single(),
                enrollment.CourseOfferingId,
                null,
                "Course",
                enrollment.CourseOffering!.Course!.Title))
            .ToListAsync(cancellationToken);

        var programStudents = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.ProgramOfferingId != null
                && programOfferingIds.Contains(enrollment.ProgramOfferingId.Value)
                && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new InstructorStudentDto(
                enrollment.UserId,
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                dbContext.Users.Where(user => user.Id == enrollment.UserId)
                    .Select(user => user.Email!).Single(),
                null,
                enrollment.ProgramOfferingId,
                "Program",
                enrollment.ProgramOffering!.Program!.Title))
            .ToListAsync(cancellationToken);

        return courseStudents
            .Concat(programStudents)
            .OrderBy(student => student.LearnerName)
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetInstructorUserIdsForOfferingAsync(
        Guid? courseOfferingId,
        Guid? programOfferingId,
        CancellationToken cancellationToken)
    {
        if (courseOfferingId.HasValue)
        {
            return await dbContext.CourseSessions.AsNoTracking()
                .Where(session => session.CourseOfferingId == courseOfferingId.Value && session.InstructorUserId != null)
                .Select(session => session.InstructorUserId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        if (programOfferingId.HasValue)
        {
            return await dbContext.ProgramSessions.AsNoTracking()
                .Where(session => session.ProgramOfferingId == programOfferingId.Value && session.InstructorUserId != null)
                .Select(session => session.InstructorUserId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return [];
    }
}
