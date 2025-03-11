using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface ITimeEntryService
    {
        Task<TimeEntry> GetTimeEntryAsync(int id);
        Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForUserAsync(string userId);
        Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForPeriodAsync(string userId, DateTime startDate, DateTime endDate);
        Task<TimeEntry> AddTimeEntryAsync(TimeEntry timeEntry);
        Task UpdateTimeEntryAsync(TimeEntry timeEntry);
        Task DeleteTimeEntryAsync(int id);
        decimal CalculateEarnings(TimeEntry timeEntry, UserSettings settings);
        TimeSpan CalculateWorkHours(TimeEntry timeEntry);
        Task<(DateTime StartDate, DateTime EndDate)> GetCurrentBillingPeriodAsync(string userId);
        Task<decimal> GetTotalEarningsForPeriodAsync(string userId, DateTime startDate, DateTime endDate);
        Task<decimal> CalculateEarningsAsync(TimeEntry timeEntry);
        Task<IEnumerable<TimeEntry>> CheckTimeEntryOverlap(string userId, DateTime workDate, TimeSpan startTime, TimeSpan endTime, int? currentEntryId = null);
    }
}
