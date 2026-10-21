using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class ProgramOfferingConfiguration : IEntityTypeConfiguration<ProgramOffering>
{
    public void Configure(EntityTypeBuilder<ProgramOffering> builder)
    {
        builder.ToTable("ProgramOfferings", table =>
        {
            table.HasCheckConstraint("CK_ProgramOfferings_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
            table.HasCheckConstraint(
                "CK_ProgramOfferings_ApplicationWindow",
                "[ApplicationOpensAtUtc] < [ApplicationClosesAtUtc] AND [ApplicationClosesAtUtc] <= [StartsAtUtc]");
            table.HasCheckConstraint("CK_ProgramOfferings_Capacity", "[Capacity] > 0");
            table.HasCheckConstraint("CK_ProgramOfferings_Price", "[Price] >= 0");
            table.HasCheckConstraint(
                "CK_ProgramOfferings_Status",
                "[Status] IN ('Draft', 'Open', 'Closed', 'Cancelled')");
        });

        builder.HasKey(offering => offering.Id);
        builder.Property(offering => offering.Price).HasPrecision(10, 2);
        builder.Property(offering => offering.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(offering => offering.Status).HasMaxLength(32).IsRequired();

        builder.HasIndex(offering => new { offering.ProgramId, offering.StartsAtUtc }).IsUnique();
        builder.HasIndex(offering => new { offering.ProgramId, offering.Status, offering.StartsAtUtc });
        builder.HasIndex(offering => new { offering.LocationId, offering.Status, offering.StartsAtUtc });

        builder.HasOne(offering => offering.Program)
            .WithMany()
            .HasForeignKey(offering => offering.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(offering => offering.Location)
            .WithMany()
            .HasForeignKey(offering => offering.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
