using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class MinijobSettingsService : IMinijobSettingsService
    {
        private readonly IMinijobSettingsRepository _minijobSettingsRepository;

        public MinijobSettingsService(IMinijobSettingsRepository minijobSettingsRepository)
        {
            _minijobSettingsRepository = minijobSettingsRepository;
        }

        public async Task<MinijobSettings> GetCurrentSettingsAsync()
        {
            var settings = await _minijobSettingsRepository.GetCurrentSettingsAsync();

            // If no settings exist, create default settings
            if (settings == null)
            {
                settings = new MinijobSettings
                {
                    MonthlyLimit = 538.0m,
                    Description = "Standardmäßige Minijob-Grenze",
                    ValidFrom = DateTime.Today,
                    IsActive = true
                };

                await _minijobSettingsRepository.AddAsync(settings);
                await _minijobSettingsRepository.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<MinijobSettings> GetSettingsForDateAsync(DateTime date)
        {
            var settings = await _minijobSettingsRepository.GetSettingsForDateAsync(date);

            // Wenn keine Einstellungen für das Datum existieren, verwende die aktuellen Einstellungen
            if (settings == null)
            {
                // Aber nur, wenn das angegebene Datum nicht in der Zukunft liegt
                if (date <= DateTime.Today)
                {
                    settings = await GetCurrentSettingsAsync();
                }
                else
                {
                    // Für zukünftige Daten: Finde die letzte gültige Einstellung vor diesem Datum
                    settings = await _minijobSettingsRepository.GetLastValidSettingsBeforeDateAsync(date);

                    // Wenn immer noch nichts gefunden wurde, Standardeinstellungen verwenden
                    if (settings == null)
                    {
                        settings = await GetCurrentSettingsAsync();
                    }
                }
            }

            return settings;
        }

        public async Task<IReadOnlyList<MinijobSettings>> GetSettingsHistoryAsync()
        {
            return await _minijobSettingsRepository.GetSettingsHistoryAsync();
        }

        public async Task<MinijobSettings> UpdateSettingsAsync(MinijobSettings settings)
        {
            // Wenn wir die aktive Einstellung aktualisieren, deaktivieren wir alle anderen Einstellungen
            if (settings.IsActive)
            {
                var currentSettings = await _minijobSettingsRepository.GetCurrentSettingsAsync();
                if (currentSettings != null && currentSettings.Id != settings.Id)
                {
                    currentSettings.IsActive = false;
                    currentSettings.ValidTo = settings.ValidFrom.AddDays(-1);
                    _minijobSettingsRepository.Update(currentSettings);
                }
            }

            // Wenn es eine neue Einstellung ist
            if (settings.Id == 0)
            {
                await _minijobSettingsRepository.AddAsync(settings);
            }
            else
            {
                // Bestehende Einstellung aktualisieren
                var existingSettings = await _minijobSettingsRepository.GetByIdAsync(settings.Id);
                if (existingSettings != null)
                {
                    // Aktualisiere die Eigenschaften der existierenden Entität
                    existingSettings.MonthlyLimit = settings.MonthlyLimit;
                    existingSettings.Description = settings.Description;
                    existingSettings.ValidFrom = settings.ValidFrom;
                    existingSettings.ValidTo = settings.ValidTo;
                    existingSettings.IsActive = settings.IsActive;
                    existingSettings.UpdatedAt = DateTime.Now;

                    // Da wir die vorhandene Entität aktualisieren, ist sie bereits vom DbContext verfolgt
                    _minijobSettingsRepository.Update(existingSettings);

                    // Aktualisiere die Referenz, damit Änderungen zurückgegeben werden
                    settings = existingSettings;
                }
            }

            await _minijobSettingsRepository.SaveChangesAsync();
            return settings;
        }
    }
}