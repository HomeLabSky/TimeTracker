using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IHourlyRateService
    {
        Task<decimal> GetRateForDateAsync(string userId, DateTime date);
        Task AddRateHistoryAsync(string userId, decimal rate, DateTime validFrom);
        Task<IReadOnlyList<HourlyRateHistory>> GetRateHistoryAsync(string userId);
    }
}