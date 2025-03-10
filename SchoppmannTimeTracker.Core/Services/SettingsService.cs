using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IUserSettingsRepository _settingsRepository;

        public SettingsService(IUserSettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
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
            }
            else
            {
                existingSettings.HourlyRate = settings.HourlyRate;
                existingSettings.BillingPeriodStartDay = settings.BillingPeriodStartDay;
                existingSettings.BillingPeriodEndDay = settings.BillingPeriodEndDay;
                existingSettings.InvoiceEmail = settings.InvoiceEmail;

                _settingsRepository.Update(existingSettings);
                settings = existingSettings;
            }

            await _settingsRepository.SaveChangesAsync();
            return settings;
        }
    }
}