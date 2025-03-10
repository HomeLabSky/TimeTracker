using SchoppmannTimeTracker.Core.Entities;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IUserSettingsRepository : IGenericRepository<UserSettings>
    {
        Task<UserSettings> GetSettingsByUserIdAsync(string userId);
    }
}