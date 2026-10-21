using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CourseCategoryRepository(ApplicationDbContext dbContext) : ICourseCategoryRepository
{
    public async Task<IReadOnlyList<CourseCategoryDto>> GetAllAsync(CancellationToken cancellationToken)
        => await dbContext.CourseCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CourseCategoryDto(category.Id, category.Name, category.Slug))
            .ToListAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.CourseCategories.AnyAsync(
            category => category.Slug == slug && (excludedId == null || category.Id != excludedId),
            cancellationToken);

    public void Add(CourseCategory category) => dbContext.CourseCategories.Add(category);

    public Task<CourseCategory?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.CourseCategories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
