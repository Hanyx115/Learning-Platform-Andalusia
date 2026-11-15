using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Community.Configurations;

public sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("CommunityPosts", table =>
        {
            table.HasCheckConstraint(
                "CK_CommunityPosts_ExactlyOneOffering",
                "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_CommunityPosts_PostType",
                "[PostType] IN ('Discussion', 'Announcement')");
        });
        builder.HasKey(post => post.Id);
        builder.Property(post => post.PostType).HasMaxLength(30).IsRequired();
        builder.Property(post => post.Title).HasMaxLength(200).IsRequired();
        builder.Property(post => post.Body).HasMaxLength(5000).IsRequired();
        builder.HasOne(post => post.CourseOffering).WithMany().HasForeignKey(post => post.CourseOfferingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(post => post.ProgramOffering).WithMany().HasForeignKey(post => post.ProgramOfferingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(post => post.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(post => new { post.CourseOfferingId, post.IsPinned, post.CreatedAtUtc });
        builder.HasIndex(post => new { post.ProgramOfferingId, post.IsPinned, post.CreatedAtUtc });
    }
}
