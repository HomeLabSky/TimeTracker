using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ITimeEntryRepository _timeEntryRepository;
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IHourlyRateService _hourlyRateService;

        public TimeEntryService(
            ITimeEntryRepository timeEntryRepository,
            IUserSettingsRepository settingsRepository,
            IHourlyRateService hourlyRateService)
        {
            _timeEntryRepository = timeEntryRepository;
            _settingsRepository = settingsRepository;
            _hourlyRateService = hourlyRateService;
        }

        public async Task<TimeEntry> GetTimeEntryAsync(int id)
        {
            return await _timeEntryRepository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForUserAsync(string userId)
        {
            return await _timeEntryRepository.GetTimeEntriesByUserAsync(userId);
        }

        public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesForPeriodAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _timeEntryRepository.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);
        }

        public async Task<TimeEntry> AddTimeEntryAsync(TimeEntry timeEntry)
        {
            // Validierung
            if (timeEntry.EndTime <= timeEntry.StartTime)
            {
                throw new ArgumentException("Die Endzeit muss nach der Startzeit liegen.");
            }

            await _timeEntryRepository.AddAsync(timeEntry);
            await _timeEntryRepository.SaveChangesAsync();
            return timeEntry;
        }

        public async Task UpdateTimeEntryAsync(TimeEntry timeEntry)
        {
            // Validierung
            if (timeEntry.EndTime <= timeEntry.StartTime)
            {
                throw new ArgumentException("Die Endzeit muss nach der Startzeit liegen.");
            }

            _timeEntryRepository.Update(timeEntry);
            await _timeEntryRepository.SaveChangesAsync();
        }

        public async Task DeleteTimeEntryAsync(int id)
        {
            var timeEntry = await _timeEntryRepository.GetByIdAsync(id);
            if (timeEntry != null)
            {
                _timeEntryRepository.Delete(timeEntry);
                await _timeEntryRepository.SaveChangesAsync();
            }
        }

        public decimal CalculateEarnings(TimeEntry timeEntry, UserSettings settings)
        {
            var workHours = CalculateWorkHours(timeEntry);

            // Immer den aktuellen Stundenlohn verwenden, unabhängig vom Datum des Zeiteintrags
            // Dies führt zu einer rückwirkenden Anwendung des neuen Stundenlohns
            decimal hourlyRate = settings.HourlyRate;

            return (decimal)workHours.TotalHours * hourlyRate;
        }

        public TimeSpan CalculateWorkHours(TimeEntry timeEntry)
        {
            return timeEntry.EndTime - timeEntry.StartTime;
        }

        public async Task<(DateTime StartDate, DateTime EndDate)> GetCurrentBillingPeriodAsync(string userId)
        {
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);

            if (settings == null)
            {
                // Standardeinstellungen verwenden (1. bis letzter Tag des Monats)
                var now = DateTime.Now;
                return (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)));
            }

            var today = DateTime.Today;
            int startDay = settings.BillingPeriodStartDay;
            int endDay = settings.BillingPeriodEndDay;

            // Startdatum berechnen
            DateTime startDate;
            if (today.Day >= startDay)
            {
                // Wir befinden uns in der aktuellen Periode
                startDate = new DateTime(today.Year, today.Month, startDay);
            }
            else
            {
                // Wir befinden uns in der vorherigen Periode
                startDate = today.AddMonths(-1);
                startDate = new DateTime(startDate.Year, startDate.Month, startDay);
            }

            // Enddatum berechnen
            DateTime endDate;
            if (endDay >= 28)
            {
                // Wenn der Endtag am Monatsende liegt, nehmen wir den letzten Tag des Monats
                var lastDayOfMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                endDay = Math.Min(endDay, lastDayOfMonth);
            }

            if (startDay <= endDay)
            {
                // Periode innerhalb eines Monats
                endDate = new DateTime(startDate.Year, startDate.Month, endDay);
            }
            else
            {
                // Periode erstreckt sich über Monatsgrenze
                endDate = startDate.AddMonths(1);
                var lastDayOfMonth = DateTime.DaysInMonth(endDate.Year, endDate.Month);
                endDay = Math.Min(endDay, lastDayOfMonth);
                endDate = new DateTime(endDate.Year, endDate.Month, endDay);
            }

            return (startDate, endDate);
        }

        public async Task<decimal> GetTotalEarningsForPeriodAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var timeEntries = await _timeEntryRepository.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);

            decimal hourlyRate = settings?.HourlyRate ?? 30.0m; // Standardwert, falls keine Einstellungen

            decimal totalEarnings = 0;
            foreach (var entry in timeEntries)
            {
                totalEarnings += await CalculateEarningsAsync(entry);
            }

            return totalEarnings;
        }

        // Neue asynchrone Methode für die Berechnung
        public async Task<decimal> CalculateEarningsAsync(TimeEntry timeEntry)
        {
            var workHours = CalculateWorkHours(timeEntry);
            System.Diagnostics.Debug.WriteLine($"CalculateEarningsAsync: Berechne Verdienst für Eintrag vom {timeEntry.WorkDate:dd.MM.yyyy} (Benutzer {timeEntry.UserId})");
            System.Diagnostics.Debug.WriteLine($"CalculateEarningsAsync: Arbeitszeit: {workHours.TotalHours} Stunden");

            // Den korrekten Stundenlohn für das Datum des Zeiteintrags abrufen
            decimal hourlyRate = await _hourlyRateService.GetRateForDateAsync(timeEntry.UserId, timeEntry.WorkDate);
            System.Diagnostics.Debug.WriteLine($"CalculateEarningsAsync: Verwendeter Stundenlohn: {hourlyRate} €");

            decimal earnings = (decimal)workHours.TotalHours * hourlyRate;
            System.Diagnostics.Debug.WriteLine($"CalculateEarningsAsync: Berechneter Verdienst: {earnings} €");

            return earnings;
        }
    }
}
