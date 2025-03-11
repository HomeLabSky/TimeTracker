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
}
