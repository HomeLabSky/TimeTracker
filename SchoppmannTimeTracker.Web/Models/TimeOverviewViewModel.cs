using System;
using System.Collections.Generic;

namespace SchoppmannTimeTracker.Web.Models
{
    public class TimeOverviewViewModel
    {
        public IEnumerable<TimeEntryListItemViewModel> TimeEntries { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalEarnings { get; set; }
        public TimeSpan TotalWorkingHours { get; set; }
        public decimal MinijobLimit { get; set; }
        public decimal CarryoverIn { get; set; }
        public decimal CarryoverOut { get; set; }
        public decimal ReportedEarnings { get; set; }
        public bool IsOverMinijobLimit { get; set; }
        public int CurrentYear { get; set; }
        public int CurrentMonth { get; set; }
        public List<BillingPeriodViewModel> BillingPeriods { get; set; }
    }

    public class TimeEntryListItemViewModel
    {
        public int Id { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan WorkingHours => EndTime - StartTime;
        public decimal Earnings { get; set; }
    }

    public class BillingPeriodViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DisplayName { get; set; }
        public bool IsCurrent => DateTime.Now >= StartDate && DateTime.Now <= EndDate;
    }
}
