using SchoppmannTimeTracker.Core.Entities;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<UserSettings> GetUserSettingsAsync(string userId);
        Task<UserSettings> CreateOrUpdateSettingsAsync(UserSettings settings);
    }
}