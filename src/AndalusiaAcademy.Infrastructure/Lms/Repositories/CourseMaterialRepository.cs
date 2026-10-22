using System.Linq.Expressions;
using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Lms.Repositories;

public sealed class CourseMaterialRepository(ApplicationDbContext dbContext)
    : ICourseMaterialRepository
{
    private static readonly Expression<Func<CourseMaterial, CourseMaterialDto>> MaterialProjection =
        material => new CourseMaterialDto(
            material.Id,
            material.CourseId,
            material.CourseSessionId,
            material.CourseSession != null ? material.CourseSession.Title : null,
            material.Title,
            material.Description,
            material.ResourceUrl,
            material.MaterialType,
            material.SortOrder,
            material.Visibility,
            material.CreatedAtUtc,
            material.UpdatedAtUtc);

    public Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken)
        => dbContext.Courses.AnyAsync(course => course.Id == courseId, cancellationToken);

    public Task<bool> IsInstructorForCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(
            session => session.InstructorUserId == userId && session.CourseOffering!.CourseId == courseId,
            cancellationToken);

    public Task<bool> SessionBelongsToCourseAsync(
        Guid courseSessionId,
        Guid courseId,
        CancellationToken cancellationToken)
        => dbContext.CourseSessions.AnyAsync(
            session => session.Id == courseSessionId
                && session.CourseOffering!.CourseId == courseId,
            cancellationToken);

    public Task<CourseMaterial?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseMaterials.SingleOrDefaultAsync(
            material => material.Id == id,
            cancellationToken);

    public Task<CourseMaterialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseMaterials
            .AsNoTracking()
            .Where(material => material.Id == id)
            .Select(MaterialProjection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CourseMaterialDto>> GetForCourseManagementAsync(
        Guid courseId,
        CancellationToken cancellationToken)
        => await dbContext.CourseMaterials
            .AsNoTracking()
            .Where(material => material.CourseId == courseId)
            .OrderBy(material => material.SortOrder)
            .ThenBy(material => material.Title)
            .Select(MaterialProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CourseMaterialDto>?> GetForLearnerEnrollmentAsync(
        Guid enrollmentId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Enrollments
            .AsNoTracking()
            .Where(item => item.Id == enrollmentId
                && item.UserId == userId
                && item.Status != EnrollmentStatus.Cancelled)
            .Select(item => new
            {
                CourseId = item.CourseOffering != null
                    ? (Guid?)item.CourseOffering.CourseId
                    : null,
                ProgramId = item.ProgramOffering != null
                    ? (Guid?)item.ProgramOffering.ProgramId
                    : null,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (enrollment is null)
        {
            return null;
        }

        var materials = dbContext.CourseMaterials
            .AsNoTracking()
            .Where(material => material.Visibility == CourseMaterialVisibilities.Published);

        materials = enrollment.CourseId.HasValue
            ? materials.Where(material => material.CourseId == enrollment.CourseId.Value)
            : materials.Where(material => dbContext.ProgramCourses.Any(programCourse =>
                programCourse.ProgramId == enrollment.ProgramId!.Value
                && programCourse.CourseId == material.CourseId));

        return await materials
            .OrderBy(material => material.CourseId)
            .ThenBy(material => material.SortOrder)
            .ThenBy(material => material.Title)
            .Select(MaterialProjection)
            .ToListAsync(cancellationToken);
    }

    public void Add(CourseMaterial material)
        => dbContext.CourseMaterials.Add(material);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
