using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class EarningsCarryoverService : IEarningsCarryoverService
    {
        private readonly IEarningsCarryoverRepository _carryoverRepository;
        private readonly ITimeEntryService _timeEntryService;
        private readonly IMinijobSettingsService _minijobSettingsService;

        public EarningsCarryoverService(
            IEarningsCarryoverRepository carryoverRepository,
            ITimeEntryService timeEntryService,
            IMinijobSettingsService minijobSettingsService)
        {
            _carryoverRepository = carryoverRepository;
            _timeEntryService = timeEntryService;
            _minijobSettingsService = minijobSettingsService;
        }

        public async Task<EarningsCarryover> GetCarryoverAsync(string userId, int year, int month)
        {
            var carryover = await _carryoverRepository.GetCarryoverAsync(userId, year, month);

            // If no carryover exists for this month, calculate it
            if (carryover == null)
            {
                carryover = await CalculateCarryoverAsync(userId, year, month);
            }

            return carryover;
        }

        public async Task<IReadOnlyList<EarningsCarryover>> GetUserCarryoverHistoryAsync(string userId)
        {
            return await _carryoverRepository.GetUserCarryoverHistoryAsync(userId);
        }

        public async Task<decimal> GetCarryoverIntoMonthAsync(string userId, int year, int month)
        {
            var carryover = await GetCarryoverAsync(userId, year, month);
            return carryover?.CarryoverIn ?? 0;
        }

        public async Task<decimal> GetCarryoverOutOfMonthAsync(string userId, int year, int month)
        {
            var carryover = await GetCarryoverAsync(userId, year, month);
            return carryover?.CarryoverOut ?? 0;
        }

        public async Task<(decimal reportedEarnings, decimal carryoverIn, decimal carryoverOut)> GetEarningsSummaryAsync(string userId, int year, int month)
        {
            var carryover = await GetCarryoverAsync(userId, year, month);

            if (carryover == null)
            {
                return (0, 0, 0);
            }

            return (carryover.ReportedEarnings, carryover.CarryoverIn, carryover.CarryoverOut);
        }

        public async Task<bool> IsUserOverLimitAsync(string userId, int year, int month)
        {
            var carryover = await GetCarryoverAsync(userId, year, month);
            return carryover?.CarryoverOut > 0;
        }

        public async Task<EarningsCarryover> CalculateCarryoverAsync(string userId, int year, int month)
        {
            // Get date range for the month
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get minijob limit for the period
            var settings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);
            decimal monthlyLimit = settings.MonthlyLimit;

            // Get existing carryover or create new one
            var carryover = await _carryoverRepository.GetCarryoverAsync(userId, year, month);
            bool isNewCarryover = carryover == null;

            if (isNewCarryover)
            {
                carryover = new EarningsCarryover
                {
                    UserId = userId,
                    Year = year,
                    Month = month
                };
            }

            // Get carryover from previous month
            var previousCarryover = await _carryoverRepository.GetPreviousMonthCarryoverAsync(userId, year, month);
            carryover.CarryoverIn = previousCarryover?.CarryoverOut ?? 0;

            // Get actual earnings from time entries for this month
            decimal actualMonthEarnings = await _timeEntryService.GetTotalEarningsForPeriodAsync(userId, startDate, endDate);

            // Total earnings including carryover from previous month
            carryover.TotalEarnings = actualMonthEarnings + carryover.CarryoverIn;

            // Apply minijob limit
            if (carryover.TotalEarnings > monthlyLimit)
            {
                carryover.ReportedEarnings = monthlyLimit;
                carryover.CarryoverOut = carryover.TotalEarnings - monthlyLimit;
            }
            else
            {
                carryover.ReportedEarnings = carryover.TotalEarnings;
                carryover.CarryoverOut = 0;
            }

            // Save the carryover
            if (isNewCarryover)
            {
                await _carryoverRepository.AddAsync(carryover);
            }
            else
            {
                _carryoverRepository.Update(carryover);
            }

            await _carryoverRepository.SaveChangesAsync();

            return carryover;
        }
    }
}