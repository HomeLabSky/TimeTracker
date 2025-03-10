using Microsoft.EntityFrameworkCore;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Infrastructure.Data;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Infrastructure.Repositories
{
    public class UserSettingsRepository : GenericRepository<UserSettings>, IUserSettingsRepository
    {
        public UserSettingsRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserSettings> GetSettingsByUserIdAsync(string userId)
        {
            return await _context.UserSettings
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}