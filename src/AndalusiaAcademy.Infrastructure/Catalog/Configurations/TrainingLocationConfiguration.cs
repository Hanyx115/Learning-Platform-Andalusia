using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class TrainingLocationConfiguration : IEntityTypeConfiguration<TrainingLocation>
{
    public void Configure(EntityTypeBuilder<TrainingLocation> builder)
    {
        builder.ToTable("TrainingLocations", table =>
            table.HasCheckConstraint("CK_TrainingLocations_Capacity", "[Capacity] > 0"));

        builder.HasKey(location => location.Id);
        builder.Property(location => location.BranchName).HasMaxLength(150).IsRequired();
        builder.Property(location => location.RoomName).HasMaxLength(100).IsRequired();
        builder.Property(location => location.City).HasMaxLength(100).IsRequired();
        builder.Property(location => location.Address).HasMaxLength(500).IsRequired();
        builder.Property(location => location.MapUrl).HasMaxLength(500);

        builder.HasIndex(location => new { location.City, location.IsActive });
        builder.HasIndex(location => new { location.BranchName, location.RoomName, location.City }).IsUnique();
    }
}
