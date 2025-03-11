using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

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
            Debug.WriteLine($"GetCarryoverAsync: Retrieving carryover for user {userId}, {year}-{month}");

            // Try to get existing carryover
            var carryover = await _carryoverRepository.GetCarryoverAsync(userId, year, month);

            // If no carryover exists for this month, calculate it
            if (carryover == null)
            {
                Debug.WriteLine($"GetCarryoverAsync: No carryover found for {year}-{month}, calculating new one");
                carryover = await CalculateCarryoverAsync(userId, year, month);
            }
            else
            {
                Debug.WriteLine($"GetCarryoverAsync: Found existing carryover for {year}-{month} with CarryoverIn={carryover.CarryoverIn}, CarryoverOut={carryover.CarryoverOut}");

                // Make sure the existing carryover has correct data from previous month
                // This helps fix any situations where carryovers were calculated out of order
                await EnsureCarryoverConsistencyAsync(carryover);
            }

            return carryover;
        }

        private async Task EnsureCarryoverConsistencyAsync(EarningsCarryover carryover)
        {
            try
            {
                // Calculate previous month
                int prevMonth = carryover.Month - 1;
                int prevYear = carryover.Year;

                if (prevMonth == 0)
                {
                    prevMonth = 12;
                    prevYear = carryover.Year - 1;
                }

                // Get previous month's carryover
                var previousCarryover = await _carryoverRepository.GetCarryoverAsync(carryover.UserId, prevYear, prevMonth);

                // If previous month exists, make sure the CarryoverIn matches the previous month's CarryoverOut
                if (previousCarryover != null && carryover.CarryoverIn != previousCarryover.CarryoverOut)
                {
                    Debug.WriteLine($"EnsureCarryoverConsistencyAsync: Fixing inconsistent carryover. Changing CarryoverIn from {carryover.CarryoverIn} to {previousCarryover.CarryoverOut}");
                    carryover.CarryoverIn = previousCarryover.CarryoverOut;

                    // Recalculate totals based on the new CarryoverIn
                    // Get date range for the month
                    var startDate = new DateTime(carryover.Year, carryover.Month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    // Get minijob limit for this period
                    var settings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);
                    decimal monthlyLimit = settings.MonthlyLimit;

                    // Get actual earnings for this month
                    decimal actualMonthEarnings = await _timeEntryService.GetTotalEarningsForPeriodAsync(
                        carryover.UserId, startDate, endDate);

                    // Total earnings including updated carryover from previous month
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

                    // Save the updated carryover
                    _carryoverRepository.Update(carryover);
                    await _carryoverRepository.SaveChangesAsync();

                    Debug.WriteLine($"EnsureCarryoverConsistencyAsync: Updated carryover saved with TotalEarnings={carryover.TotalEarnings}, ReportedEarnings={carryover.ReportedEarnings}, CarryoverOut={carryover.CarryoverOut}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureCarryoverConsistencyAsync: Error ensuring consistency: {ex.Message}");
                // Log error but don't throw it - we don't want to crash the application
            }
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
            Debug.WriteLine($"GetEarningsSummaryAsync: Getting summary for user {userId}, {year}-{month}");
            var carryover = await GetCarryoverAsync(userId, year, month);

            if (carryover == null)
            {
                Debug.WriteLine("GetEarningsSummaryAsync: No carryover data found, returning zeros");
                return (0, 0, 0);
            }

            Debug.WriteLine($"GetEarningsSummaryAsync: Returning summary data: reported={carryover.ReportedEarnings}, in={carryover.CarryoverIn}, out={carryover.CarryoverOut}");
            return (carryover.ReportedEarnings, carryover.CarryoverIn, carryover.CarryoverOut);
        }

        public async Task<bool> IsUserOverLimitAsync(string userId, int year, int month)
        {
            var carryover = await GetCarryoverAsync(userId, year, month);
            return carryover?.CarryoverOut > 0;
        }

        public async Task<EarningsCarryover> CalculateCarryoverAsync(string userId, int year, int month)
        {
            Debug.WriteLine($"CalculateCarryoverAsync: Calculating carryover for user {userId}, {year}-{month}");

            // Get date range for the month
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get minijob limit for the period
            var settings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);
            decimal monthlyLimit = settings.MonthlyLimit;
            Debug.WriteLine($"CalculateCarryoverAsync: Monthly limit is {monthlyLimit} €");

            // Get existing carryover or create new one
            var carryover = await _carryoverRepository.GetCarryoverAsync(userId, year, month);
            bool isNewCarryover = carryover == null;

            if (isNewCarryover)
            {
                Debug.WriteLine("CalculateCarryoverAsync: Creating new carryover entity");
                carryover = new EarningsCarryover
                {
                    UserId = userId,
                    Year = year,
                    Month = month
                };
            }
            else
            {
                Debug.WriteLine("CalculateCarryoverAsync: Updating existing carryover entity");
            }

            // IMPORTANT: Always calculate previous month's carryover first if needed
            // This ensures we have the correct carryover chain
            decimal carryoverInAmount = await GetPreviousMonthCarryoverOutAsync(userId, year, month);
            Debug.WriteLine($"CalculateCarryoverAsync: Carryover from previous month: {carryoverInAmount} €");

            // Set the carryover from previous month
            carryover.CarryoverIn = carryoverInAmount;

            // Get actual earnings from time entries for this month
            decimal actualMonthEarnings = await _timeEntryService.GetTotalEarningsForPeriodAsync(userId, startDate, endDate);
            Debug.WriteLine($"CalculateCarryoverAsync: Actual month earnings: {actualMonthEarnings} €");

            // Total earnings including carryover from previous month
            carryover.TotalEarnings = actualMonthEarnings + carryover.CarryoverIn;
            Debug.WriteLine($"CalculateCarryoverAsync: Total earnings (actual + carryover): {carryover.TotalEarnings} €");

            // Apply minijob limit
            if (carryover.TotalEarnings > monthlyLimit)
            {
                carryover.ReportedEarnings = monthlyLimit;
                carryover.CarryoverOut = carryover.TotalEarnings - monthlyLimit;
                Debug.WriteLine($"CalculateCarryoverAsync: Over limit! Reported: {carryover.ReportedEarnings} €, Carryover out: {carryover.CarryoverOut} €");
            }
            else
            {
                carryover.ReportedEarnings = carryover.TotalEarnings;
                carryover.CarryoverOut = 0;
                Debug.WriteLine($"CalculateCarryoverAsync: Under limit. Reported: {carryover.ReportedEarnings} €, Carryover out: {carryover.CarryoverOut} €");
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
            Debug.WriteLine($"CalculateCarryoverAsync: Saved carryover with ID {carryover.Id}");

            return carryover;
        }

        // Helper method to ensure we get the previous month's carryover out value correctly
        private async Task<decimal> GetPreviousMonthCarryoverOutAsync(string userId, int year, int month)
        {
            try
            {
                // Calculate previous month
                int prevMonth = month - 1;
                int prevYear = year;

                if (prevMonth == 0)
                {
                    prevMonth = 12;
                    prevYear = year - 1;
                }

                Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: Calculating previous month: {prevYear}-{prevMonth}");

                // Try to get the previous month's carryover
                var previousCarryover = await _carryoverRepository.GetCarryoverAsync(userId, prevYear, prevMonth);

                // If previous month's carryover doesn't exist, calculate it first
                if (previousCarryover == null)
                {
                    Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: No previous carryover found, calculating {prevYear}-{prevMonth} first");

                    // For months before the current one, we need to calculate the carryover
                    // But don't go too far back in time - only calculate if it's reasonably recent
                    DateTime currentDate = DateTime.Today;
                    DateTime requestedPrevDate = new DateTime(prevYear, prevMonth, 1);

                    // Only calculate for the past 12 months to avoid too much recursive calculation
                    if (currentDate.AddMonths(-12) <= requestedPrevDate)
                    {
                        // Recursively calculate the previous month's carryover
                        previousCarryover = await CalculateCarryoverAsync(userId, prevYear, prevMonth);
                        Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: Calculated previous month with CarryoverOut={previousCarryover.CarryoverOut}");
                    }
                    else
                    {
                        // For older months, create an empty carryover without calculating
                        Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: Month too far in past, creating empty carryover");
                        previousCarryover = new EarningsCarryover
                        {
                            UserId = userId,
                            Year = prevYear,
                            Month = prevMonth,
                            CarryoverIn = 0,
                            CarryoverOut = 0,
                            TotalEarnings = 0,
                            ReportedEarnings = 0
                        };

                        await _carryoverRepository.AddAsync(previousCarryover);
                        await _carryoverRepository.SaveChangesAsync();
                    }
                }
                else
                {
                    Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: Found previous carryover with CarryoverOut={previousCarryover.CarryoverOut}");
                }

                return previousCarryover.CarryoverOut;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetPreviousMonthCarryoverOutAsync: Error getting previous carryover: {ex.Message}");
                // If there's any error in the process, return 0 as a fallback
                return 0;
            }
        }
    }
}