using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IEarningsCarryoverService
    {
        Task<EarningsCarryover> GetCarryoverAsync(string userId, int year, int month);
        Task<EarningsCarryover> CalculateCarryoverAsync(string userId, int year, int month);
        Task<IReadOnlyList<EarningsCarryover>> GetUserCarryoverHistoryAsync(string userId);
        Task<decimal> GetCarryoverIntoMonthAsync(string userId, int year, int month);
        Task<decimal> GetCarryoverOutOfMonthAsync(string userId, int year, int month);
        Task<(decimal reportedEarnings, decimal carryoverIn, decimal carryoverOut)> GetEarningsSummaryAsync(string userId, int year, int month);
        Task<bool> IsUserOverLimitAsync(string userId, int year, int month);
    }
}