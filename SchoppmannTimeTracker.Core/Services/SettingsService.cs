using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IHourlyRateService _hourlyRateService;

        public SettingsService(
            IUserSettingsRepository settingsRepository,
            IHourlyRateService hourlyRateService)
        {
            _settingsRepository = settingsRepository;
            _hourlyRateService = hourlyRateService;
        }

        public async Task<UserSettings> GetUserSettingsAsync(string userId)
        {
            var settings = await _settingsRepository.GetSettingsByUserIdAsync(userId);

            // Wenn keine Einstellungen existieren, erstelle Standardeinstellungen
            if (settings == null)
            {
                settings = new UserSettings
                {
                    UserId = userId,
                    HourlyRate = 30.0m,
                    BillingPeriodStartDay = 1,
                    BillingPeriodEndDay = 31,
                    InvoiceEmail = string.Empty
                };

                await _settingsRepository.AddAsync(settings);
                await _settingsRepository.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<UserSettings> CreateOrUpdateSettingsAsync(UserSettings settings)
        {
            var existingSettings = await _settingsRepository.GetSettingsByUserIdAsync(settings.UserId);

            if (existingSettings == null)
            {
                await _settingsRepository.AddAsync(settings);

                // Füge den initialen Stundenlohn zur Historie hinzu
                await _hourlyRateService.AddRateHistoryAsync(
                    settings.UserId,
                    settings.HourlyRate,
                    settings.HourlyRateValidFrom);
            }
            else
            {
                // Prüfen, ob sich der Stundenlohn geändert hat
                if (existingSettings.HourlyRate != settings.HourlyRate)
                {
                    // Neuen Eintrag in der Stundenlohn-Historie erstellen
                    await _hourlyRateService.AddRateHistoryAsync(
                        settings.UserId,
                        settings.HourlyRate,
                        settings.HourlyRateValidFrom);
                }

                existingSettings.HourlyRate = settings.HourlyRate;
                existingSettings.BillingPeriodStartDay = settings.BillingPeriodStartDay;
                existingSettings.BillingPeriodEndDay = settings.BillingPeriodEndDay;
                existingSettings.InvoiceEmail = settings.InvoiceEmail;
                existingSettings.HourlyRateValidFrom = settings.HourlyRateValidFrom;

                _settingsRepository.Update(existingSettings);
                settings = existingSettings;
            }

            await _settingsRepository.SaveChangesAsync();
            return settings;
        }
    }
}