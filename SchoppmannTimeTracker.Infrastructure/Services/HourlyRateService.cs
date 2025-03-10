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

            // Alle Rateneinträge für den Benutzer abrufen und nach Gültigkeitsdatum sortieren
            var allRates = await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);

            // Die zum angegebenen Datum gültige Rate finden
            var validRate = allRates
                .Where(r => r.ValidFrom <= date && (r.ValidTo == null || r.ValidTo >= date))
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault();

            if (validRate != null)
            {
                System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Historische Rate gefunden: {validRate.Rate} € (gültig von {validRate.ValidFrom:dd.MM.yyyy} bis {(validRate.ValidTo.HasValue ? validRate.ValidTo.Value.ToString("dd.MM.yyyy") : "heute")})");
                return validRate.Rate;
            }

            // Wenn keine Rate für das Datum gefunden wurde, die aktuellen Einstellungen verwenden
            // Aber nur wenn das Datum nach oder gleich dem Gültigkeitsdatum ist
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);
            if (settings != null)
            {
                // Wenn das Datum VOR dem Gültigkeitsdatum der aktuellen Einstellungen liegt,
                // dann verwenden wir nicht den aktuellen Stundenlohn
                if (date < settings.HourlyRateValidFrom)
                {
                    // In diesem Fall suchen wir die nächstälteste Rate vor diesem Datum
                    var oldestPreviousRate = allRates
                        .Where(r => r.ValidFrom <= date)
                        .OrderByDescending(r => r.ValidFrom)
                        .FirstOrDefault();

                    if (oldestPreviousRate != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Vorherige Rate gefunden: {oldestPreviousRate.Rate} € (gültig von {oldestPreviousRate.ValidFrom:dd.MM.yyyy})");
                        return oldestPreviousRate.Rate;
                    }

                    // Wenn es keine älteren Raten gibt, nehmen wir einen Standardwert
                    System.Diagnostics.Debug.WriteLine($"GetRateForDateAsync: Keine historische Rate gefunden, verwende Standardwert 30.0 €");
                    return 30.0m;
                }

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

            // Alle bestehenden Raten holen
            var existingRates = await _hourlyRateRepository.GetRateHistoryByUserIdAsync(userId);
            System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: {existingRates.Count} bestehende Raten gefunden");

            // Den letzten aktiven Eintrag finden (der ohne ValidTo)
            var activeRate = existingRates.FirstOrDefault(r => r.ValidTo == null);

            if (activeRate != null && activeRate.ValidFrom < validFrom)
            {
                var newValidTo = validFrom.AddDays(-1);
                System.Diagnostics.Debug.WriteLine($"AddRateHistoryAsync: Aktualisiere ValidTo auf {newValidTo:dd.MM.yyyy} für Rate {activeRate.Rate} €");

                activeRate.ValidTo = newValidTo;
                _hourlyRateRepository.Update(activeRate);
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