using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class HourlyRateService : IHourlyRateService
    {
        private readonly IHourlyRateRepository _hourlyRateRepository;
        private readonly IUserSettingsRepository _settingsRepository;

        public HourlyRateService(
            IHourlyRateRepository hourlyRateRepository,
            IUserSettingsRepository settingsRepository)
        {
            _hourlyRateRepository = hourlyRateRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<decimal> GetRateForDateAsync(string userId, DateTime date)
        {
            // Zuerst versuchen, einen historischen Eintrag für das Datum zu finden
            var historicalRate = await _hourlyRateRepository.GetRateForDateAsync(userId, date);
            if (historicalRate != null)
            {
                return historicalRate.Rate;
            }

            // Wenn kein historischer Eintrag gefunden wurde, die aktuellen Einstellungen verwenden
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);
            if (settings != null)
            {
                return settings.HourlyRate;
            }

            // Standardwert, falls keine Einstellungen gefunden wurden
            return 30.0m;
        }

        public async Task AddRateHistoryAsync(string userId, decimal rate, DateTime validFrom)
        {
            // Zuerst den aktuellen Eintrag (falls vorhanden) beenden
            var existingRates = await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);
            foreach (var existingRate in existingRates)
            {
                if (existingRate.ValidTo == null && existingRate.ValidFrom < validFrom)
                {
                    existingRate.ValidTo = validFrom.AddSeconds(-1);
                    _hourlyRateRepository.Update(existingRate);
                }
            }

            // Neuen Eintrag hinzufügen
            var newRateHistory = new HourlyRateHistory
            {
                UserId = userId,
                Rate = rate,
                ValidFrom = validFrom,
                ValidTo = null
            };

            await _hourlyRateRepository.AddAsync(newRateHistory);
            await _hourlyRateRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<HourlyRateHistory>> GetRateHistoryAsync(string userId)
        {
            return await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);
        }
    }
}