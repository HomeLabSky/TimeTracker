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

            // If no settings exist for the date, use current settings
            if (settings == null)
            {
                settings = await GetCurrentSettingsAsync();
            }

            return settings;
        }

        public async Task<IReadOnlyList<MinijobSettings>> GetSettingsHistoryAsync()
        {
            return await _minijobSettingsRepository.GetSettingsHistoryAsync();
        }

        public async Task<MinijobSettings> UpdateSettingsAsync(MinijobSettings settings)
        {
            // If we're updating the active setting, deactivate all other settings
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

            // If it's a new setting
            if (settings.Id == 0)
            {
                await _minijobSettingsRepository.AddAsync(settings);
            }
            else
            {
                _minijobSettingsRepository.Update(settings);
            }

            await _minijobSettingsRepository.SaveChangesAsync();
            return settings;
        }
    }
}