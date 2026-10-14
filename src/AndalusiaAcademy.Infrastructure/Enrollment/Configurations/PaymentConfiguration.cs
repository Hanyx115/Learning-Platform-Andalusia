using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(payment => payment.CreatedAt)
            .IsRequired();

        builder.HasIndex(payment => payment.IdempotencyKey)
            .IsUnique();

        builder.HasOne(payment => payment.Application)
            .WithMany()
            .HasForeignKey(payment => payment.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One open/succeeded payment per application at a time; a Failed payment doesn't block retrying.
        builder.HasIndex(payment => payment.ApplicationId)
            .HasDatabaseName("IX_Payments_Application_Active")
            .HasFilter("[Status] <> 'Failed'")
            .IsUnique();
    }
}
