using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Repositories;

public sealed class CourseRepository(ApplicationDbContext dbContext) : ICourseRepository
{
    public async Task<PaginatedResult<CourseDto>> GetPageAsync(
        CourseCatalogQuery query,
        CancellationToken cancellationToken)
    {
        var courses = dbContext.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            courses = courses.Where(course =>
                course.Title.Contains(search) ||
                course.Description.Contains(search) ||
                course.Category!.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            courses = courses.Where(course => course.Category!.Slug == category);
        }

        // Defaults to Published only, so a Draft/Archived course never leaks into public browsing
        // unless a caller explicitly asks for another status (staff management views).
        courses = string.IsNullOrWhiteSpace(query.Status)
            ? courses.Where(course => course.Status == CatalogStatuses.Published)
            : courses.Where(course => course.Status == query.Status.Trim());

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = query.Type.Trim();
            courses = courses.Where(course => course.Type == type);
        }

        var totalCount = await courses.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.PageSize;

        if (offset > int.MaxValue)
        {
            return new PaginatedResult<CourseDto>([], query.Page, query.PageSize, totalCount);
        }

        var orderedCourses = query.Sort.ToLowerInvariant() switch
        {
            CourseCatalogQuery.TitleDescendingSort => courses
                .OrderByDescending(course => course.Title)
                .ThenBy(course => course.Id),
            CourseCatalogQuery.NewestSort => courses
                .OrderByDescending(course => course.CreatedAt)
                .ThenBy(course => course.Id),
            CourseCatalogQuery.OldestSort => courses
                .OrderBy(course => course.CreatedAt)
                .ThenBy(course => course.Id),
            _ => courses
                .OrderBy(course => course.Title)
                .ThenBy(course => course.Id),
        };

        var items = await orderedCourses
            .Select(course => new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                Status = course.Status,
                Type = course.Type,
                Price = course.Price,
                CategorySlug = course.Category!.Slug,
                CategoryName = course.Category!.Name,
                CreatedAt = course.CreatedAt,
            })
            .Skip((int)offset)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CourseDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Slug == slug && course.Status == CatalogStatuses.Published)
            .Select(course => new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                Status = course.Status,
                Type = course.Type,
                Price = course.Price,
                CategorySlug = course.Category!.Slug,
                CategoryName = course.Category!.Name,
                CreatedAt = course.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
        => dbContext.CourseCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.Courses.AnyAsync(
            course => course.Slug == slug && (excludedId == null || course.Id != excludedId),
            cancellationToken);

    public void Add(Course course) => dbContext.Courses.Add(course);

    public Task<Course?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Courses.FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    public Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Id == id)
            .Select(course => new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                Status = course.Status,
                Type = course.Type,
                Price = course.Price,
                CategorySlug = course.Category!.Slug,
                CategoryName = course.Category!.Name,
                CreatedAt = course.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
