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
            // Debug-Ausgabe
            System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Suche Rate für Benutzer {userId} am {date:dd.MM.yyyy}");

            // Zuerst versuchen, einen historischen Eintrag für das Datum zu finden
            var historicalRate = await _hourlyRateRepository.GetRateForDateAsync(userId, date);
            if (historicalRate != null)
            {
                System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Historische Rate gefunden: {historicalRate.Rate} € (gültig von {historicalRate.ValidFrom:dd.MM.yyyy} bis {(historicalRate.ValidTo.HasValue ? historicalRate.ValidTo.Value.ToString("dd.MM.yyyy") : "heute")})");
                return historicalRate.Rate;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("GetRateForDateAsync: Keine historische Rate gefunden, verwende aktuelle Einstellungen");
            }

            // Wenn kein historischer Eintrag gefunden wurde, die aktuellen Einstellungen verwenden
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);
            if (settings != null)
            {
                System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Verwende aktuellen Stundenlohn aus Settings: {settings.HourlyRate} € (gültig ab {settings.HourlyRateValidFrom:dd.MM.yyyy})");
                return settings.HourlyRate;
            }

            // Standardwert, falls keine Einstellungen gefunden wurden
            System.Diagnostics.Debug.WriteLine("GetRateForDateAsync: Keine Einstellungen gefunden, verwende Standardwert 30.0 €");
            return 30.0m;
        }

        public async Task AddRateHistoryAsync(string userId, decimal rate, DateTime validFrom)
        {
            System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: Füge neue Rate {rate} € für Benutzer {userId} ab {validFrom:dd.MM.yyyy} hinzu");
            // Zuerst den aktuellen Eintrag (falls vorhanden) beenden
            var existingRates = await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);
            System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: {existingRates.Count} bestehende Raten gefunden");

            foreach (var existingRate in existingRates)
            {
                System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: Prüfe bestehende Rate: {existingRate.Rate} € (gültig von {existingRate.ValidFrom:dd.MM.yyyy} bis {(existingRate.ValidTo.HasValue ? existingRate.ValidTo.Value.ToString("dd.MM.yyyy") : "heute")})");
                
                if (existingRate.ValidTo == null && existingRate.ValidFrom < validFrom)
                {
                    var newValidTo = validFrom.AddDays(-1);
                    System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: Aktualisiere ValidTo auf {newValidTo:dd.MM.yyyy} für Rate {existingRate.Rate} €");

                    existingRate.ValidTo = validFrom.AddDays(-1);
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
            System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: Füge neuen Eintrag hinzu: {rate} € ab {validFrom:dd.MM.yyyy}");
            await _hourlyRateRepository.AddAsync(newRateHistory);
            await _hourlyRateRepository.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<HourlyRateHistory>> GetRateHistoryAsync(string userId)
        {
            return await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);
        }
    }
}