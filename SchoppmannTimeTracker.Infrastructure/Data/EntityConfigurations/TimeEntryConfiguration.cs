using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data.EntityConfigurations
{
    public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
    {
        public void Configure(EntityTypeBuilder<TimeEntry> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.WorkDate)
                .IsRequired();

            builder.Property(t => t.StartTime)
                .IsRequired();

            builder.Property(t => t.EndTime)
                .IsRequired();

            // Beziehung zu User
            builder.HasOne(t => t.User)
                .WithMany(u => u.TimeEntries)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}