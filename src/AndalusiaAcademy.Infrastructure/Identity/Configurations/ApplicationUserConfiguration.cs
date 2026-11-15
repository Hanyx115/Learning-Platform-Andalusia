using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Identity.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(user => user.PreferredLanguage)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(user => user.LearnerPetEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.LearnerPetType)
            .HasMaxLength(20)
            .HasDefaultValue("cat")
            .IsRequired();

        builder.Property(user => user.ConsentedAt)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .HasFilter("[NormalizedEmail] IS NOT NULL")
            .IsUnique();
    }
}
