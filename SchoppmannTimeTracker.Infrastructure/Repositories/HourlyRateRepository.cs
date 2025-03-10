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
    public class HourlyRateRepository : GenericRepository<HourlyRateHistory>, IHourlyRateRepository
    {
        public HourlyRateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<HourlyRateHistory>> GetRateHistoryByUserIdAsync(string userId)
        {
            return await _context.HourlyRateHistory
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ValidFrom)
                .ToListAsync();
        }

        public async Task<HourlyRateHistory> GetRateForDateAsync(string userId, DateTime date)
        {
            return await _context.HourlyRateHistory
                .Where(x => x.UserId == userId && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date))
                .OrderByDescending(x => x.ValidFrom)
                .FirstOrDefaultAsync();
        }
    }
}