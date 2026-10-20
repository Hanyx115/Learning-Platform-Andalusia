using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CourseOfferingConfiguration : IEntityTypeConfiguration<CourseOffering>
{
    public void Configure(EntityTypeBuilder<CourseOffering> builder)
    {
        builder.ToTable("CourseOfferings", table =>
        {
            table.HasCheckConstraint("CK_CourseOfferings_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
            table.HasCheckConstraint(
                "CK_CourseOfferings_ApplicationWindow",
                "[ApplicationOpensAtUtc] < [ApplicationClosesAtUtc] AND [ApplicationClosesAtUtc] <= [StartsAtUtc]");
            table.HasCheckConstraint("CK_CourseOfferings_Capacity", "[Capacity] > 0");
            table.HasCheckConstraint("CK_CourseOfferings_Price", "[Price] >= 0");
            table.HasCheckConstraint(
                "CK_CourseOfferings_Status",
                "[Status] IN ('Draft', 'Open', 'Closed', 'Cancelled')");
        });

        builder.HasKey(offering => offering.Id);
        builder.Property(offering => offering.Price).HasPrecision(10, 2);
        builder.Property(offering => offering.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(offering => offering.Status).HasMaxLength(32).IsRequired();

        builder.HasIndex(offering => new { offering.CourseId, offering.StartsAtUtc }).IsUnique();
        builder.HasIndex(offering => new { offering.CourseId, offering.Status, offering.StartsAtUtc });

        builder.HasOne(offering => offering.Course)
            .WithMany()
            .HasForeignKey(offering => offering.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
