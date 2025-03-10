using SchoppmannTimeTracker.Core.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Core.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateTimeSheetPdfAsync(string userId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateAllUsersTimeSheetPdfAsync(DateTime startDate, DateTime endDate);
    }
}