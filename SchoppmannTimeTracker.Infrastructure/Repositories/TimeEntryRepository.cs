using Microsoft.EntityFrameworkCore;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Infrastructure.Repositories
{
    public class TimeEntryRepository : GenericRepository<TimeEntry>, ITimeEntryRepository
    {
        public TimeEntryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesByUserAsync(string userId)
        {
            return await _context.TimeEntries
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForPeriodAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeEntries
                .Where(x => x.UserId == userId && x.WorkDate >= startDate && x.WorkDate <= endDate)
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForDateAsync(string userId, DateTime workDate)
        {
            return await _context.TimeEntries
                .Where(x => x.UserId == userId && x.WorkDate.Date == workDate.Date)
                .OrderBy(x => x.StartTime)
                .ToListAsync();
        }
    }
}