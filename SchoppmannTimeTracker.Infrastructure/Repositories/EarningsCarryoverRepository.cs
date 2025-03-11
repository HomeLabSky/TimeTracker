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
    public class EarningsCarryoverRepository : GenericRepository<EarningsCarryover>, IEarningsCarryoverRepository
    {
        public EarningsCarryoverRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<EarningsCarryover> GetCarryoverAsync(string userId, int year, int month)
        {
            return await _context.EarningsCarryovers
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Year == year && c.Month == month);
        }

        public async Task<IReadOnlyList<EarningsCarryover>> GetUserCarryoverHistoryAsync(string userId)
        {
            return await _context.EarningsCarryovers
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .ToListAsync();
        }

        public async Task<EarningsCarryover> GetPreviousMonthCarryoverAsync(string userId, int year, int month)
        {
            // Calculate previous month
            int prevMonth = month - 1;
            int prevYear = year;

            if (prevMonth == 0)
            {
                prevMonth = 12;
                prevYear = year - 1;
            }

            return await _context.EarningsCarryovers
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Year == prevYear && c.Month == prevMonth);
        }
    }
}