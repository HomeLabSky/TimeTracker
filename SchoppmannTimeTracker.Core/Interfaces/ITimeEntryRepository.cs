using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface ITimeEntryRepository : IGenericRepository<TimeEntry>
    {
        Task<IReadOnlyList<TimeEntry>> GetTimeEntriesByUserAsync(string userId);
        Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForPeriodAsync(string userId, DateTime startDate, DateTime endDate);
    }
}