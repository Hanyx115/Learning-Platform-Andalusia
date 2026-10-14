using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class PaymentStatusHistoryConfiguration : IEntityTypeConfiguration<PaymentStatusHistory>
{
    public void Configure(EntityTypeBuilder<PaymentStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(history => history.Source)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(history => history.ChangedAt)
            .IsRequired();

        builder.HasOne(history => history.Payment)
            .WithMany()
            .HasForeignKey(history => history.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => history.PaymentId);
    }
}
