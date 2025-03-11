using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data.EntityConfigurations
{
    public class MinijobSettingsConfiguration : IEntityTypeConfiguration<MinijobSettings>
    {
        public void Configure(EntityTypeBuilder<MinijobSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.MonthlyLimit)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(s => s.ValidFrom)
                .IsRequired();

            builder.Property(s => s.IsActive)
                .IsRequired();
        }
    }
}
