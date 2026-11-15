using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Content.Seeding;

public static class CmsDemoSeeder
{
    private const string DemoInstructorEmail = "instructor.demo@andalusia.test";
    private const string DemoInstructorKey = "demo-instructor";

    public static async Task<int> SeedInstructorProfileAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        var instructor = await userManager.FindByEmailAsync(DemoInstructorEmail);
        if (instructor is null || await dbContext.CmsContentItems.AnyAsync(
                item => item.ContentType == CmsContentTypes.InstructorProfile && item.Key == DemoInstructorKey,
                cancellationToken))
        {
            return 0;
        }

        var seededAt = DateTimeOffset.UtcNow;
        dbContext.CmsContentItems.Add(new CmsContentItem
        {
            Id = Guid.NewGuid(),
            ContentType = CmsContentTypes.InstructorProfile,
            Key = DemoInstructorKey,
            TitleEn = $"{instructor.FirstName} {instructor.LastName}",
            TitleAr = "المدرب التجريبي",
            BodyEn = "An Andalusia Academy instructor focused on practical, learner-centred training and clear professional outcomes.",
            BodyAr = "مدرب في أكاديمية أندلسية يركز على التدريب العملي ودعم المتعلمين وتحقيق نتائج مهنية واضحة.",
            LinkUrl = "https://www.linkedin.com/company/andalusia-academy",
            RelatedUserId = instructor.Id,
            DisplayOrder = 10,
            IsPublished = true,
            CreatedAtUtc = seededAt,
            UpdatedAtUtc = seededAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return 1;
    }
}
