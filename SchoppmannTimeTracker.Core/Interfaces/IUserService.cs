using SchoppmannTimeTracker.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IUserService
    {
        Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<bool> CreateUserAsync(ApplicationUser user, string password, string role);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> AddUserToRoleAsync(string userId, string role);
        Task<bool> RemoveUserFromRoleAsync(string userId, string role);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<Dictionary<string, IList<string>>> GetUserRolesMappingAsync(IEnumerable<string> userIds);
    }
}