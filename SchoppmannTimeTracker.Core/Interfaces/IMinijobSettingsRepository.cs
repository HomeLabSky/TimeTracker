using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IMinijobSettingsRepository : IGenericRepository<MinijobSettings>
    {
        Task<MinijobSettings> GetCurrentSettingsAsync();
        Task<MinijobSettings> GetSettingsForDateAsync(DateTime date);
        Task<IReadOnlyList<MinijobSettings>> GetSettingsHistoryAsync();
    }

    public interface IEarningsCarryoverRepository : IGenericRepository<EarningsCarryover>
    {
        Task<EarningsCarryover> GetCarryoverAsync(string userId, int year, int month);
        Task<IReadOnlyList<EarningsCarryover>> GetUserCarryoverHistoryAsync(string userId);
        Task<EarningsCarryover> GetPreviousMonthCarryoverAsync(string userId, int year, int month);
    }
}