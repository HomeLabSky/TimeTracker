using System;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class EarningsCarryover : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        // Carryover from previous month
        public decimal CarryoverIn { get; set; } = 0.0m;

        // Carryover to next month
        public decimal CarryoverOut { get; set; } = 0.0m;

        // Total earnings before applying limits (actual work + carryover in)
        public decimal TotalEarnings { get; set; } = 0.0m;

        // Earnings reported for this month (limited by minijob threshold)
        public decimal ReportedEarnings { get; set; } = 0.0m;
    }
}