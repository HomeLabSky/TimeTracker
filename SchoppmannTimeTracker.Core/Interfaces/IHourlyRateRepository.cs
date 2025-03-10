using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IHourlyRateRepository : IGenericRepository<HourlyRateHistory>
    {
        Task<IReadOnlyList<HourlyRateHistory>> GetRateHistoryByUserIdAsync(string userId);
        Task<HourlyRateHistory> GetRateForDateAsync(string userId, DateTime date);
    }
}