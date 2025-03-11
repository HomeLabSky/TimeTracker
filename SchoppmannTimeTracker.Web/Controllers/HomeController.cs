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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfService _pdfService;
        private readonly IEarningsCarryoverService _earningsCarryoverService;
        private readonly IMinijobSettingsService _minijobSettingsService;

        public HomeController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager,
            IPdfService pdfService,
            IEarningsCarryoverService earningsCarryoverService,
            IMinijobSettingsService minijobSettingsService)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _userManager = userManager;
            _pdfService = pdfService;
            _earningsCarryoverService = earningsCarryoverService;
            _minijobSettingsService = minijobSettingsService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            System.Diagnostics.Debug.WriteLine($"Abrechnungszeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);

            System.Diagnostics.Debug.WriteLine($"Anzahl Zeiteinträge: {timeEntries.Count}");

            // Minijob-Informationen abrufen
            var year = startDate.Year;
            var month = startDate.Month;
            var earningsSummary = await _earningsCarryoverService.GetEarningsSummaryAsync(userId, year, month);
            var minijobSettings = await _minijobSettingsService.GetSettingsForDateAsync(DateTime.Today);

            System.Diagnostics.Debug.WriteLine($"Minijob-Grenze: {minijobSettings.MonthlyLimit:N2} €");
            System.Diagnostics.Debug.WriteLine($"Carryover In: {earningsSummary.carryoverIn:N2} €, Carryover Out: {earningsSummary.carryoverOut:N2} €");

            decimal totalEarnings = 0;
            TimeSpan totalWorkHours = TimeSpan.Zero;
            var viewEntries = new List<TimeEntryListItemViewModel>();

            foreach (var entry in timeEntries)
            {
                var workHours = _timeEntryService.CalculateWorkHours(entry);
                var earnings = await _timeEntryService.CalculateEarningsAsync(entry);

                System.Diagnostics.Debug.WriteLine($"Eintrag {entry.Id}: {entry.WorkDate:dd.MM.yyyy}, {workHours:hh\\:mm}, {earnings:N2} €");

                viewEntries.Add(new TimeEntryListItemViewModel
                {
                    Id = entry.Id,
                    WorkDate = entry.WorkDate,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Earnings = earnings
                });

                totalEarnings += earnings;
                totalWorkHours += workHours;
            }

            System.Diagnostics.Debug.WriteLine($"Gesamtarbeitszeit: {totalWorkHours:hh\\:mm}, Gesamtverdienst: {totalEarnings:N2} €");

            var viewModel = new TimeOverviewViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TimeEntries = viewEntries.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.StartTime),
                TotalEarnings = totalEarnings,
                TotalWorkingHours = totalWorkHours,
                // Minijob-Informationen
                MinijobLimit = minijobSettings.MonthlyLimit,
                CarryoverIn = earningsSummary.carryoverIn,
                CarryoverOut = earningsSummary.carryoverOut,
                ReportedEarnings = earningsSummary.reportedEarnings,
                IsOverMinijobLimit = earningsSummary.carryoverOut > 0
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            System.Diagnostics.Debug.WriteLine($"Generiere PDF für Zeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

            var pdfBytes = await _pdfService.GenerateTimeSheetPdfAsync(userId, startDate, endDate);
            var user = await _userManager.FindByIdAsync(userId);
            var fileName = $"Zeiterfassung_{user.LastName}_{startDate:yyyy-MM}.pdf";

            System.Diagnostics.Debug.WriteLine($"PDF generiert für Benutzer {user.FirstName} {user.LastName}, Dateiname: {fileName}");

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateAllUsersPdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            System.Diagnostics.Debug.WriteLine($"Generiere PDF für alle Benutzer, Zeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

            var pdfBytes = await _pdfService.GenerateAllUsersTimeSheetPdfAsync(startDate, endDate);
            var fileName = $"Zeiterfassung_Alle_Mitarbeiter_{startDate:yyyy-MM}.pdf";

            System.Diagnostics.Debug.WriteLine($"PDF für alle Benutzer generiert, Dateiname: {fileName}");

            return File(pdfBytes, "application/pdf", fileName);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}