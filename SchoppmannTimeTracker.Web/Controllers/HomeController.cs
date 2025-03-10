using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ISettingsService _settingsService;
        private readonly IPdfService _pdfService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHourlyRateService _hourlyRateService; // Neu hinzugef�gt

        public HomeController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            IPdfService pdfService,
            UserManager<ApplicationUser> userManager,
            IHourlyRateService hourlyRateService) // Neu hinzugef�gt
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _pdfService = pdfService;
            _userManager = userManager;
            _hourlyRateService = hourlyRateService; // Neu hinzugef�gt
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);
            var userSettings = await _settingsService.GetUserSettingsAsync(userId);

            // Modifizierte Berechnung der Eintr�ge mit historischen Stundenl�hnen
            var viewModel = new TimeOverviewViewModel
            {
                TimeEntries = await MapTimeEntriesToViewModel(timeEntries, userId),
                StartDate = startDate,
                EndDate = endDate,
                TotalEarnings = await CalculateTotalEarningsAsync(timeEntries, userId),
                TotalWorkingHours = CalculateTotalWorkingHours(timeEntries)
            };

            return View(viewModel);
        }

        // Neue Hilfsmethode zur Berechnung der Gesamtverg�tung mit historischen Stundenl�hnen
        private async Task<decimal> CalculateTotalEarningsAsync(IReadOnlyList<TimeEntry> timeEntries, string userId)
        {
            decimal totalEarnings = 0;
            foreach (var entry in timeEntries)
            {
                totalEarnings += await _timeEntryService.CalculateEarningsAsync(entry);
            }
            return totalEarnings;
        }

        // Hilfsmethode zur Berechnung der Gesamtarbeitszeit
        private TimeSpan CalculateTotalWorkingHours(IReadOnlyList<TimeEntry> timeEntries)
        {
            var totalHours = TimeSpan.Zero;
            foreach (var entry in timeEntries)
            {
                totalHours += _timeEntryService.CalculateWorkHours(entry);
            }
            return totalHours;
        }

        // Neue Hilfsmethode zum Mapping von TimeEntries auf ViewModel-Objekte
        private async Task<IEnumerable<TimeEntryListItemViewModel>> MapTimeEntriesToViewModel(
            IReadOnlyList<TimeEntry> timeEntries, string userId)
        {
            var result = new List<TimeEntryListItemViewModel>();

            foreach (var entry in timeEntries)
            {
                // Berechnung des historischen Stundenlohns f�r diesen Eintrag
                var earnings = await _timeEntryService.CalculateEarningsAsync(entry);

                result.Add(new TimeEntryListItemViewModel
                {
                    Id = entry.Id,
                    WorkDate = entry.WorkDate,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Earnings = earnings
                });
            }

            return result.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.StartTime);
        }

        public async Task<IActionResult> GeneratePdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            var pdfBytes = await _pdfService.GenerateTimeSheetPdfAsync(userId, startDate, endDate);
            return File(pdfBytes, "application/pdf", $"Timesheet_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateAllUsersPdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            var pdfBytes = await _pdfService.GenerateAllUsersTimeSheetPdfAsync(startDate, endDate);
            return File(pdfBytes, "application/pdf", $"AllUsers_Timesheet_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}