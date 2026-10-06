using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CareerPathProgramConfiguration : IEntityTypeConfiguration<CareerPathProgram>
{
    public void Configure(EntityTypeBuilder<CareerPathProgram> builder)
    {
        builder.HasKey(link => new { link.CareerPathId, link.ProgramId });

        builder.HasOne(link => link.CareerPath)
            .WithMany()
            .HasForeignKey(link => link.CareerPathId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Program)
            .WithMany()
            .HasForeignKey(link => link.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
