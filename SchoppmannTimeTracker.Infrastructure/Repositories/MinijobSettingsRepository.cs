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
    public class MinijobSettingsRepository : GenericRepository<MinijobSettings>, IMinijobSettingsRepository
    {
        public MinijobSettingsRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<MinijobSettings> GetCurrentSettingsAsync()
        {
            return await _context.MinijobSettings
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.ValidFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<MinijobSettings> GetSettingsForDateAsync(DateTime date)
        {
            return await _context.MinijobSettings
                .Where(s => s.ValidFrom <= date && (s.ValidTo == null || s.ValidTo >= date))
                .OrderByDescending(s => s.ValidFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<MinijobSettings>> GetSettingsHistoryAsync()
        {
            return await _context.MinijobSettings
                .OrderByDescending(s => s.ValidFrom)
                .ToListAsync();
        }
    }
}