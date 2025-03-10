using System;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class TimeEntry : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Berechnete Eigenschaften
        public TimeSpan WorkingHours => EndTime - StartTime;
    }
}