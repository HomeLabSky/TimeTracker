using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IMinijobSettingsService
    {
        Task<MinijobSettings> GetCurrentSettingsAsync();
        Task<MinijobSettings> GetSettingsForDateAsync(DateTime date);
        Task<MinijobSettings> UpdateSettingsAsync(MinijobSettings settings);
        Task<IReadOnlyList<MinijobSettings>> GetSettingsHistoryAsync();
    }
}