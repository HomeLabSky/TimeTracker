using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoppmannTimeTracker.Core.Entities;

namespace SchoppmannTimeTracker.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<HourlyRateHistory> HourlyRateHistory { get; set; }
        public DbSet<MinijobSettings> MinijobSettings { get; set; }
        public DbSet<EarningsCarryover> EarningsCarryovers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Wichtig! Diese Zeile nicht entfernen!

            // Anwenden der Entity-Konfigurationen
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}