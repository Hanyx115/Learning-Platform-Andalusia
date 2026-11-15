using AndalusiaAcademy.Application.DTOs.Skills;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class SkillsPassportRepository(ApplicationDbContext dbContext) : ISkillsPassportRepository
{
    public async Task<SkillsPassportDto> GetForLearnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var enrollments = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId && enrollment.Status == EnrollmentStatus.Active)
            .Select(enrollment => new EnrollmentEvidenceScope(
                enrollment.CourseOfferingId,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.CourseId : null,
                enrollment.CourseOffering != null ? enrollment.CourseOffering.Course!.Title : null,
                enrollment.ProgramOfferingId,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.ProgramId : null,
                enrollment.ProgramOffering != null ? enrollment.ProgramOffering.Program!.Title : null))
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return new SkillsPassportDto(0, 0, 0, []);
        }

        var courseIds = enrollments.Where(item => item.CourseId.HasValue).Select(item => item.CourseId!.Value).Distinct().ToList();
        var programIds = enrollments.Where(item => item.ProgramId.HasValue).Select(item => item.ProgramId!.Value).Distinct().ToList();
        var mappings = await dbContext.LearningSkillMappings
            .AsNoTracking()
            .Where(mapping => mapping.Skill!.IsActive
                && ((mapping.CourseId.HasValue && courseIds.Contains(mapping.CourseId.Value))
                    || (mapping.ProgramId.HasValue && programIds.Contains(mapping.ProgramId.Value))))
            .Select(mapping => new MappingRow(
                mapping.SkillId,
                mapping.Skill!.Name,
                mapping.Skill.Category,
                mapping.Skill.Description,
                mapping.TargetLevel,
                mapping.CourseId,
                mapping.ProgramId))
            .ToListAsync(cancellationToken);

        var courseOfferingIds = enrollments.Where(item => item.CourseOfferingId.HasValue)
            .Select(item => item.CourseOfferingId!.Value).Distinct().ToList();
        var programOfferingIds = enrollments.Where(item => item.ProgramOfferingId.HasValue)
            .Select(item => item.ProgramOfferingId!.Value).Distinct().ToList();
        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => (assignment.CourseOfferingId.HasValue && courseOfferingIds.Contains(assignment.CourseOfferingId.Value))
                || (assignment.ProgramOfferingId.HasValue && programOfferingIds.Contains(assignment.ProgramOfferingId.Value)))
            .Select(assignment => new { assignment.Id, assignment.CourseOfferingId, assignment.ProgramOfferingId, assignment.MaxScore })
            .ToListAsync(cancellationToken);
        var assignmentIds = assignments.Select(item => item.Id).ToList();
        var submissions = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(submission => submission.UserId == userId
                && submission.Score.HasValue
                && assignmentIds.Contains(submission.AssignmentId))
            .Select(submission => new { submission.AssignmentId, Score = submission.Score!.Value, submission.GradedAtUtc })
            .ToListAsync(cancellationToken);
        var submissionsByAssignment = submissions.ToDictionary(item => item.AssignmentId);

        var passportSkills = mappings
            .GroupBy(mapping => mapping.SkillId)
            .Select(skillGroup =>
            {
                var evidence = new List<SkillEvidenceDto>();
                foreach (var mapping in skillGroup)
                {
                    foreach (var enrollment in enrollments.Where(item =>
                        (mapping.CourseId.HasValue && item.CourseId == mapping.CourseId)
                        || (mapping.ProgramId.HasValue && item.ProgramId == mapping.ProgramId)))
                    {
                        var graded = assignments
                            .Where(assignment => assignment.CourseOfferingId == enrollment.CourseOfferingId
                                && assignment.ProgramOfferingId == enrollment.ProgramOfferingId)
                            .Where(assignment => submissionsByAssignment.ContainsKey(assignment.Id))
                            .Select(assignment => new
                            {
                                Percentage = submissionsByAssignment[assignment.Id].Score / assignment.MaxScore * 100m,
                                submissionsByAssignment[assignment.Id].GradedAtUtc,
                            })
                            .ToList();
                        if (graded.Count == 0)
                        {
                            continue;
                        }

                        evidence.Add(new SkillEvidenceDto(
                            enrollment.CourseTitle ?? enrollment.ProgramTitle ?? "Learning offering",
                            enrollment.CourseOfferingId.HasValue ? "Course" : "Program",
                            enrollment.CourseOfferingId ?? enrollment.ProgramOfferingId!.Value,
                            graded.Count,
                            Math.Round(graded.Average(item => item.Percentage), 2),
                            graded.Max(item => item.GradedAtUtc)));
                    }
                }

                var evidenceCount = evidence.Sum(item => item.GradedAssignments);
                var mastery = evidenceCount == 0
                    ? (decimal?)null
                    : Math.Round(evidence.Sum(item => item.AverageScorePercentage * item.GradedAssignments) / evidenceCount, 2);
                var first = skillGroup.First();
                return new PassportSkillDto(
                    first.SkillId,
                    first.Name,
                    first.Category,
                    first.Description,
                    skillGroup.Max(item => item.TargetLevel),
                    mastery,
                    mastery is null ? "NotAssessed" : mastery >= 75m ? "Demonstrated" : "Developing",
                    evidenceCount,
                    evidence.OrderByDescending(item => item.LastGradedAtUtc).ToList());
            })
            .OrderByDescending(skill => skill.MasteryPercentage.HasValue)
            .ThenByDescending(skill => skill.MasteryPercentage)
            .ThenBy(skill => skill.Name)
            .ToList();

        return new SkillsPassportDto(
            passportSkills.Count(skill => skill.MasteryPercentage.HasValue),
            passportSkills.Count(skill => skill.Status == "Developing"),
            passportSkills.Count(skill => skill.Status == "Demonstrated"),
            passportSkills);
    }

    private sealed record EnrollmentEvidenceScope(
        Guid? CourseOfferingId, Guid? CourseId, string? CourseTitle,
        Guid? ProgramOfferingId, Guid? ProgramId, string? ProgramTitle);

    private sealed record MappingRow(
        Guid SkillId, string Name, string Category, string Description, int TargetLevel, Guid? CourseId, Guid? ProgramId);
}
