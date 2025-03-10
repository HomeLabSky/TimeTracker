using System;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class HourlyRateHistory : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public decimal Rate { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}