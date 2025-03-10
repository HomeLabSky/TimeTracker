using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data.EntityConfigurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.HourlyRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(s => s.BillingPeriodStartDay)
                .IsRequired();

            builder.Property(s => s.BillingPeriodEndDay)
                .IsRequired();

            builder.Property(s => s.InvoiceEmail)
                .HasMaxLength(256);

            // 1:1-Beziehung zu User
            builder.HasOne(s => s.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}