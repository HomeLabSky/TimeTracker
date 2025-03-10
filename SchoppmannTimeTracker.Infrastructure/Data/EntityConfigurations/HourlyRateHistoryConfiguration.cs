using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data.EntityConfigurations
{
    public class HourlyRateHistoryConfiguration : IEntityTypeConfiguration<HourlyRateHistory>
    {
        public void Configure(EntityTypeBuilder<HourlyRateHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.Rate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(h => h.ValidFrom)
                .IsRequired();

            // Beziehung zu User
            builder.HasOne(h => h.User)
                .WithMany(u => u.RateHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}