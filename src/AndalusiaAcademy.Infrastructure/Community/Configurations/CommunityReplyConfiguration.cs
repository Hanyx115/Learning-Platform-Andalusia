using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Community.Configurations;

public sealed class CommunityReplyConfiguration : IEntityTypeConfiguration<CommunityReply>
{
    public void Configure(EntityTypeBuilder<CommunityReply> builder)
    {
        builder.ToTable("CommunityReplies");
        builder.HasKey(reply => reply.Id);
        builder.Property(reply => reply.Body).HasMaxLength(3000).IsRequired();
        builder.HasOne(reply => reply.Post).WithMany().HasForeignKey(reply => reply.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(reply => reply.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(reply => new { reply.PostId, reply.CreatedAtUtc });
    }
}
