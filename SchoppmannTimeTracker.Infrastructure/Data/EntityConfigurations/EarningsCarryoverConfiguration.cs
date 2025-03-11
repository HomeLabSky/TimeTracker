using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data.EntityConfigurations
{
    public class EarningsCarryoverConfiguration : IEntityTypeConfiguration<EarningsCarryover>
    {
        public void Configure(EntityTypeBuilder<EarningsCarryover> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Year)
                .IsRequired();

            builder.Property(e => e.Month)
                .IsRequired();

            builder.Property(e => e.CarryoverIn)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.CarryoverOut)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.TotalEarnings)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.ReportedEarnings)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Relationship to User
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create a unique index on UserId + Year + Month
            builder.HasIndex(e => new { e.UserId, e.Year, e.Month })
                .IsUnique();
        }
    }
}